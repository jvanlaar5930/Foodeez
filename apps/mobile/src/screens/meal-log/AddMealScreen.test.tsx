import React from 'react';
import { act, fireEvent, renderWithTheme, screen, waitFor } from '@/test/render';
import { AddMealScreen } from './AddMealScreen';
import { searchFoodItems } from '@/services/foodItemService';
import { useMealStore } from '@/store/mealStore';
import { useAuthStore } from '@/store/authStore';
import { MealType, type FoodItemDto } from '@/types';

jest.mock('@/services/foodItemService', () => ({
  searchFoodItems: jest.fn(async () => []),
}));

// The quick-add bar and the photo scanner each reach for the network on mount, and neither is
// what these tests are about.
jest.mock('@/components/meal/QuickAddBar', () => ({
  QuickAddBar: () => null,
}));

jest.mock('@/services/aiService', () => ({
  analyzeMealStream: jest.fn(),
  analyzeMealLogStream: jest.fn(),
}));

const searchMock = searchFoodItems as jest.MockedFunction<typeof searchFoodItems>;

function food(overrides: Partial<FoodItemDto> = {}): FoodItemDto {
  return {
    id: 'food-1',
    name: 'Rolled oats',
    servingSize: 100,
    servingUnit: 'g',
    nutritionalInfo: {
      calories: 380,
      protein: 13,
      carbohydrates: 67,
      fat: 7,
      fiber: 10,
      sugar: 1,
      sodium: 5,
    },
    ...overrides,
  } as FoodItemDto;
}

const navigation = { goBack: jest.fn(), navigate: jest.fn() };

function renderScreen(params: Record<string, unknown> = {}) {
  return renderWithTheme(
    // The screen only reads `navigation.goBack`/`navigate` and `route.params`.
    <AddMealScreen
      navigation={navigation as never}
      route={{ key: 'AddMeal', name: 'AddMeal', params } as never}
    />,
  );
}

/**
 * AddMealScreen was 847 lines doing six things. These cover the paths through it that the
 * split had to preserve: searching, picking, adjusting, totalling and saving.
 */
describe('AddMealScreen', () => {
  beforeEach(() => {
    jest.useFakeTimers();
    jest.clearAllMocks();
    searchMock.mockResolvedValue([]);

    useAuthStore.setState({ user: { id: 'user-1' } as never });
    useMealStore.setState({
      selectedDate: '2026-03-03',
      isLoading: false,
      error: null,
      logMeal: jest.fn(async () => {}),
      updateMealLog: jest.fn(async () => {}),
    } as never);
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  /** Types into the search box and lets the debounce elapse. */
  async function searchFor(text: string) {
    fireEvent.changeText(screen.getByLabelText('Search food items...'), text);
    await act(async () => {
      jest.advanceTimersByTime(400);
    });
  }

  it('opens on the meal type it was asked for', () => {
    renderScreen({ mealType: MealType.Dinner });

    expect(screen.getByText('Save Dinner')).toBeTruthy();
  });

  it('shows nothing until something is typed', () => {
    renderScreen();

    expect(screen.queryByText('0 results')).toBeNull();
    expect(searchMock).not.toHaveBeenCalled();
  });

  it('searches once typing pauses, and lists what came back', async () => {
    searchMock.mockResolvedValue([food()]);
    renderScreen();

    await searchFor('oats');

    expect(searchMock).toHaveBeenCalledWith('oats');
    expect(screen.getByText('Rolled oats')).toBeTruthy();
  });

  it('offers the homemade route when nothing matched', async () => {
    renderScreen();

    await searchFor('grandma stew');

    expect(screen.getByText(/Add "grandma stew" as a homemade food/)).toBeTruthy();
  });

  it('picking a result adds it at one serving and totals it', async () => {
    searchMock.mockResolvedValue([food()]);
    renderScreen();
    await searchFor('oats');

    fireEvent.press(screen.getByText('Rolled oats'));

    expect(screen.getByText('Selected (1)')).toBeTruthy();
    // One serving of a food labelled per 100g, so the label figures unchanged.
    expect(screen.getByText('380')).toBeTruthy();
    expect(screen.getByText('13g')).toBeTruthy();
  });

  it('the amount buttons move by one step, and the totals follow', async () => {
    searchMock.mockResolvedValue([food()]);
    renderScreen();
    await searchFor('oats');
    fireEvent.press(screen.getByText('Rolled oats'));

    // A food measured in grams steps by 25, so 100g becomes 125g and 380 kcal becomes 475.
    fireEvent.press(screen.getByLabelText('More Rolled oats'));

    expect(screen.getByDisplayValue('125')).toBeTruthy();
    expect(screen.getByText('475')).toBeTruthy();
  });

  /**
   * The stepper's floor is one step, so anything below it can only be reached by typing.
   * A loaf scanned as 10 slices per container used to step by 5 and refuse to go under it,
   * which left a two-slice sandwich impossible to log as anything but a third of a loaf.
   */
  it('takes an amount typed straight into the row', async () => {
    searchMock.mockResolvedValue([food({ name: 'White loaf', servingSize: 10, servingUnit: 'slice' })]);
    renderScreen();
    await searchFor('loaf');
    fireEvent.press(screen.getByText('White loaf'));

    const amount = screen.getByLabelText('Amount of White loaf in slice');
    fireEvent.changeText(amount, '2');
    fireEvent(amount, 'blur');

    expect(screen.getByDisplayValue('2')).toBeTruthy();
    // Two slices of ten, so a fifth of the label's 380 kcal.
    expect(screen.getByText('76')).toBeTruthy();
  });

  it('removing the last food empties the meal again', async () => {
    searchMock.mockResolvedValue([food()]);
    renderScreen();
    await searchFor('oats');
    fireEvent.press(screen.getByText('Rolled oats'));

    fireEvent.press(screen.getByLabelText('Remove Rolled oats'));

    expect(screen.queryByText('Selected (1)')).toBeNull();
  });

  it('will not save an empty meal', () => {
    renderScreen();

    fireEvent.press(screen.getByText('Save Breakfast'));

    expect(useMealStore.getState().logMeal).not.toHaveBeenCalled();
  });

  it('saves what was picked, against the store\'s selected date', async () => {
    searchMock.mockResolvedValue([food()]);
    renderScreen();
    await searchFor('oats');
    fireEvent.press(screen.getByText('Rolled oats'));

    await act(async () => {
      fireEvent.press(screen.getByText('Save Breakfast'));
    });

    await waitFor(() => expect(useMealStore.getState().logMeal).toHaveBeenCalled());
    expect(useMealStore.getState().logMeal).toHaveBeenCalledWith({
      userId: 'user-1',
      logDate: '2026-03-03',
      mealType: MealType.Breakfast,
      items: [{ foodItemId: 'food-1', quantity: 100, unit: 'g' }],
    });
    expect(navigation.goBack).toHaveBeenCalled();
  });

  it('editing keeps the meal log\'s own date rather than the tab\'s', async () => {
    // Reached from the calendar showing a different day, `selectedDate` is the wrong date.
    renderScreen({
      mealLog: {
        id: 'log-1',
        logDate: '2026-02-14',
        mealType: MealType.Lunch,
        items: [{ foodItem: food(), quantity: 100 }],
      },
    });

    await act(async () => {
      fireEvent.press(screen.getByText('Update Meal'));
    });

    await waitFor(() => expect(useMealStore.getState().updateMealLog).toHaveBeenCalled());
    expect(useMealStore.getState().updateMealLog).toHaveBeenCalledWith(
      'log-1',
      expect.objectContaining({ logDate: '2026-02-14', mealType: MealType.Lunch }),
    );
  });

  it('opens an existing meal with its items already in place', () => {
    renderScreen({
      mealLog: {
        id: 'log-1',
        logDate: '2026-02-14',
        mealType: MealType.Lunch,
        items: [{ foodItem: food(), quantity: 250 }],
      },
    });

    expect(screen.getByText('Selected (1)')).toBeTruthy();
    expect(screen.getByDisplayValue('250')).toBeTruthy();
  });

  it('fills in from a photo scan rather than clearing what is there', async () => {
    searchMock.mockResolvedValue([food()]);
    renderScreen({
      parsedItems: [
        { foodItem: food({ id: 'food-2', name: 'Banana' }), quantity: 120, source: 'Estimated' },
      ],
    });

    expect(screen.getByText('Banana')).toBeTruthy();
    expect(screen.getByText('AI estimate')).toBeTruthy();
  });
});
