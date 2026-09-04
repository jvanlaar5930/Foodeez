import React from 'react';
import { Text } from 'react-native';
import { fireEvent, renderWithTheme, screen } from '@/test/render';
import { DateRangeNav } from './DateRangeNav';
import { ErrorBanner } from './ErrorBanner';
import { MacroRow } from './MacroRow';
import { ModalActions } from './ModalActions';
import { ModalSheet } from './ModalSheet';
import { ScreenHeader } from './ScreenHeader';
import { SearchBar } from './SearchBar';

/**
 * These replace hand-written copies scattered across the screens, so what is pinned here is
 * the behaviour those copies disagreed about - whether a dialog can be dismissed mid-save,
 * whether a search can be cleared, whether you can page into days that do not exist.
 */

describe('ModalSheet', () => {
  it('renders nothing while closed', () => {
    renderWithTheme(
      <ModalSheet visible={false} onClose={jest.fn()} title="Edit meal">
        <Text>Body</Text>
      </ModalSheet>,
    );

    expect(screen.queryByText('Body')).toBeNull();
  });

  it('shows its title and body when open', () => {
    renderWithTheme(
      <ModalSheet visible onClose={jest.fn()} title="Edit meal">
        <Text>Body</Text>
      </ModalSheet>,
    );

    expect(screen.getByText('Edit meal')).toBeTruthy();
    expect(screen.getByText('Body')).toBeTruthy();
  });

  it('closes from the title bar', () => {
    const onClose = jest.fn();
    renderWithTheme(
      <ModalSheet visible onClose={onClose} title="Edit meal">
        <Text>Body</Text>
      </ModalSheet>,
    );

    fireEvent.press(screen.getByLabelText('Close'));

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('renders a footer outside the scrolling body', () => {
    renderWithTheme(
      <ModalSheet visible onClose={jest.fn()} footer={<Text>Save</Text>}>
        <Text>Body</Text>
      </ModalSheet>,
    );

    expect(screen.getByText('Save')).toBeTruthy();
  });
});

describe('ModalActions', () => {
  it('reports which button was pressed', () => {
    const onConfirm = jest.fn();
    const onCancel = jest.fn();
    renderWithTheme(
      <ModalActions confirmLabel="Save" onConfirm={onConfirm} onCancel={onCancel} />,
    );

    fireEvent.press(screen.getByText('Save'));
    fireEvent.press(screen.getByText('Cancel'));

    expect(onConfirm).toHaveBeenCalledTimes(1);
    expect(onCancel).toHaveBeenCalledTimes(1);
  });

  it('blocks both buttons while busy, so a double tap cannot send twice', () => {
    const onConfirm = jest.fn();
    const onCancel = jest.fn();
    renderWithTheme(
      <ModalActions confirmLabel="Save" onConfirm={onConfirm} onCancel={onCancel} busy />,
    );

    // The label is replaced by a spinner while busy.
    expect(screen.queryByText('Save')).toBeNull();
    fireEvent.press(screen.getByText('Cancel'));

    expect(onCancel).not.toHaveBeenCalled();
  });

  it('blocks confirm on an incomplete form but still lets you back out', () => {
    const onConfirm = jest.fn();
    const onCancel = jest.fn();
    renderWithTheme(
      <ModalActions
        confirmLabel="Save"
        onConfirm={onConfirm}
        onCancel={onCancel}
        confirmDisabled
      />,
    );

    fireEvent.press(screen.getByText('Save'));
    fireEvent.press(screen.getByText('Cancel'));

    expect(onConfirm).not.toHaveBeenCalled();
    expect(onCancel).toHaveBeenCalledTimes(1);
  });
});

describe('ErrorBanner', () => {
  it('renders nothing when there is no error', () => {
    renderWithTheme(<ErrorBanner message={null} />);

    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('shows the message, and can be dismissed when the caller offers it', () => {
    const onDismiss = jest.fn();
    renderWithTheme(<ErrorBanner message="That could not be saved." onDismiss={onDismiss} />);

    expect(screen.getByText('That could not be saved.')).toBeTruthy();
    fireEvent.press(screen.getByLabelText('Dismiss'));
    expect(onDismiss).toHaveBeenCalledTimes(1);
  });

  it('has no dismiss button when nothing would happen', () => {
    renderWithTheme(<ErrorBanner message="Something went wrong." />);

    expect(screen.queryByLabelText('Dismiss')).toBeNull();
  });
});

describe('SearchBar', () => {
  it('reports what was typed', () => {
    const onChangeText = jest.fn();
    renderWithTheme(<SearchBar value="" onChangeText={onChangeText} placeholder="Find a food" />);

    fireEvent.changeText(screen.getByLabelText('Find a food'), 'oats');

    expect(onChangeText).toHaveBeenCalledWith('oats');
  });

  it('offers a clear button only once there is something to clear', () => {
    const onChangeText = jest.fn();
    const { rerender } = renderWithTheme(<SearchBar value="" onChangeText={onChangeText} />);
    expect(screen.queryByLabelText('Clear search')).toBeNull();

    rerender(<SearchBar value="oats" onChangeText={onChangeText} />);
    fireEvent.press(screen.getByLabelText('Clear search'));

    expect(onChangeText).toHaveBeenCalledWith('');
  });

  it('shows a spinner instead of the clear button while searching', () => {
    renderWithTheme(<SearchBar value="oats" onChangeText={jest.fn()} isSearching />);

    expect(screen.queryByLabelText('Clear search')).toBeNull();
  });
});

describe('DateRangeNav', () => {
  it('pages in both directions', () => {
    const onPrevious = jest.fn();
    const onNext = jest.fn();
    renderWithTheme(<DateRangeNav label="Mon 3 Mar" onPrevious={onPrevious} onNext={onNext} />);

    fireEvent.press(screen.getByLabelText('Previous'));
    fireEvent.press(screen.getByLabelText('Next'));

    expect(onPrevious).toHaveBeenCalledTimes(1);
    expect(onNext).toHaveBeenCalledTimes(1);
  });

  it('will not page forward past what exists', () => {
    const onNext = jest.fn();
    renderWithTheme(
      <DateRangeNav label="Today" onPrevious={jest.fn()} onNext={onNext} canGoNext={false} />,
    );

    fireEvent.press(screen.getByLabelText('Next'));

    expect(onNext).not.toHaveBeenCalled();
  });

  it('offers the shortcut back only when the caller gives one', () => {
    const onReset = jest.fn();
    const { rerender } = renderWithTheme(
      <DateRangeNav label="Mon 3 Mar" onPrevious={jest.fn()} onNext={jest.fn()} />,
    );
    expect(screen.queryByText('Today')).toBeNull();

    rerender(
      <DateRangeNav
        label="Mon 3 Mar"
        onPrevious={jest.fn()}
        onNext={jest.fn()}
        onReset={onReset}
      />,
    );
    fireEvent.press(screen.getByText('Today'));

    expect(onReset).toHaveBeenCalledTimes(1);
  });
});

describe('MacroRow', () => {
  it('rounds every figure the same way', () => {
    // The copies this replaces disagreed: one showed a decimal place on protein, so the same
    // meal read as 30.6g in one screen and 31g in the next.
    renderWithTheme(
      <MacroRow totals={{ calories: 520.4, protein: 30.6, carbs: 44.2, fat: 21.9 }} />,
    );

    expect(screen.getByText('520')).toBeTruthy();
    expect(screen.getByText('31g')).toBeTruthy();
    expect(screen.getByText('44g')).toBeTruthy();
    expect(screen.getByText('22g')).toBeTruthy();
  });

  it('shows zeroes rather than blanks for an empty meal', () => {
    renderWithTheme(<MacroRow totals={{ calories: 0, protein: 0, carbs: 0, fat: 0 }} />);

    expect(screen.getByText('0')).toBeTruthy();
    expect(screen.getAllByText('0g')).toHaveLength(3);
  });
});

describe('ScreenHeader', () => {
  it('shows a title and an optional subtitle', () => {
    renderWithTheme(<ScreenHeader title="Grocery list" subtitle="3-9 March" />);

    expect(screen.getByText('Grocery list')).toBeTruthy();
    expect(screen.getByText('3-9 March')).toBeTruthy();
  });

  it('has a back button only when there is somewhere to go', () => {
    const onBack = jest.fn();
    const { rerender } = renderWithTheme(<ScreenHeader title="Grocery list" />);
    expect(screen.queryByLabelText('Go back')).toBeNull();

    rerender(<ScreenHeader title="Grocery list" onBack={onBack} />);
    fireEvent.press(screen.getByLabelText('Go back'));

    expect(onBack).toHaveBeenCalledTimes(1);
  });
});
