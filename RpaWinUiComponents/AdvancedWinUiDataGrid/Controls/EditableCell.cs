// OPRAVA 3,5: Custom EditableCell Control s Proper Validation + UI/UX
// SÚBOR: RpaWinUiComponents/AdvancedWinUiDataGrid/Controls/EditableCell.cs

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using RpaWinUiComponents.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Controls
{
    /// <summary>
    /// Custom control pre editovateľnú bunku s proper validation support
    /// OPRAVA: Nahradí manuálne TextBox vytváranie za proper control s validation
    /// </summary>
    [TemplatePart(Name = "PART_DisplayText", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_EditText", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_RootBorder", Type = typeof(Border))]
    [TemplatePart(Name = "PART_ValidationBorder", Type = typeof(Border))]
    public sealed class EditableCell : Control
    {
        private TextBlock? _displayText;
        private TextBox? _editText;
        private Border? _rootBorder;
        private Border? _validationBorder;
        private bool _isTemplateApplied;

        #region Dependency Properties

        public static readonly DependencyProperty CellViewModelProperty =
            DependencyProperty.Register(
                nameof(CellViewModel),
                typeof(CellViewModel),
                typeof(EditableCell),
                new PropertyMetadata(null, OnCellViewModelChanged));

        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register(
                nameof(IsEditing),
                typeof(bool),
                typeof(EditableCell),
                new PropertyMetadata(false, OnIsEditingChanged));

        public static readonly DependencyProperty HasValidationErrorProperty =
            DependencyProperty.Register(
                nameof(HasValidationError),
                typeof(bool),
                typeof(EditableCell),
                new PropertyMetadata(false, OnValidationErrorChanged));

        public CellViewModel? CellViewModel
        {
            get => (CellViewModel?)GetValue(CellViewModelProperty);
            set => SetValue(CellViewModelProperty, value);
        }

        public bool IsEditing
        {
            get => (bool)GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }

        public bool HasValidationError
        {
            get => (bool)GetValue(HasValidationErrorProperty);
            set => SetValue(HasValidationErrorProperty, value);
        }

        #endregion

        #region Events (OPRAVA 5: Better Focus Management)

        public event EventHandler<CellViewModel>? EditingStarted;
        public event EventHandler<CellViewModel>? EditingCompleted;
        public event EventHandler<CellViewModel>? EditingCancelled;
        public event EventHandler<(CellViewModel cell, VirtualKey key)>? KeyboardNavigation;

        #endregion

        public EditableCell()
        {
            DefaultStyleKey = typeof(EditableCell);

            // OPRAVA 5: Keyboard support zachované
            this.KeyDown += OnKeyDown;
            this.DoubleTapped += OnDoubleTapped;
            this.Tapped += OnTapped;
            this.GotFocus += OnGotFocus;
            this.LostFocus += OnLostFocus;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Unsubscribe previous controls
            if (_editText != null)
            {
                _editText.TextChanged -= OnEditTextChanged;
                _editText.LostFocus -= OnEditTextLostFocus;
                _editText.KeyDown -= OnEditTextKeyDown;
            }

            // Get template parts
            _displayText = GetTemplateChild("PART_DisplayText") as TextBlock;
            _editText = GetTemplateChild("PART_EditText") as TextBox;
            _rootBorder = GetTemplateChild("PART_RootBorder") as Border;
            _validationBorder = GetTemplateChild("PART_ValidationBorder") as Border;

            // Subscribe to new controls
            if (_editText != null)
            {
                _editText.TextChanged += OnEditTextChanged;
                _editText.LostFocus += OnEditTextLostFocus;
                _editText.KeyDown += OnEditTextKeyDown;
            }

            _isTemplateApplied = true;
            UpdateVisualState();
            UpdateValidationState();
            UpdateDisplayValue();
        }

        #region Event Handlers (OPRAVA 5: Proper Keyboard Navigation)

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (CellViewModel == null) return;

            // ZACHOVANÉ: Všetky klávesové skratky
            switch (e.Key)
            {
                case VirtualKey.F2:
                    if (!IsEditing && !CellViewModel.IsReadOnly)
                    {
                        StartEditing();
                        e.Handled = true;
                    }
                    break;

                case VirtualKey.Enter:
                    if (IsEditing)
                    {
                        CommitEditing();
                    }
                    KeyboardNavigation?.Invoke(this, (CellViewModel, e.Key));
                    e.Handled = true;
                    break;

                case VirtualKey.Escape:
                    if (IsEditing)
                    {
                        CancelEditing();
                        e.Handled = true;
                    }
                    break;

                case VirtualKey.Tab:
                    if (IsEditing)
                    {
                        CommitEditing();
                    }
                    KeyboardNavigation?.Invoke(this, (CellViewModel, e.Key));
                    break;

                case VirtualKey.Delete:
                    if (!IsEditing && !CellViewModel.IsReadOnly)
                    {
                        CellViewModel.Value = null;
                        e.Handled = true;
                    }
                    break;

                case VirtualKey.Up:
                case VirtualKey.Down:
                case VirtualKey.Left:
                case VirtualKey.Right:
                    if (!IsEditing)
                    {
                        KeyboardNavigation?.Invoke(this, (CellViewModel, e.Key));
                    }
                    break;

                default:
                    // Start editing on any printable character
                    if (!IsEditing && !CellViewModel.IsReadOnly && IsPrintableKey(e.Key))
                    {
                        StartEditing();
                        // Let the character through to the TextBox
                    }
                    break;
            }
        }

        private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (CellViewModel?.IsReadOnly == false && !IsEditing)
            {
                StartEditing();
                e.Handled = true;
            }
        }

        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (CellViewModel != null)
            {
                CellViewModel.HasFocus = true;
                this.Focus(FocusState.Programmatic);
            }
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (CellViewModel != null)
            {
                CellViewModel.HasFocus = true;
            }
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (CellViewModel != null)
            {
                CellViewModel.HasFocus = false;

                if (IsEditing)
                {
                    CommitEditing();
                }
            }
        }

        private void OnEditTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_editText != null && CellViewModel != null && IsEditing)
            {
                // ZACHOVANÉ: Real-time validation trigger
                CellViewModel.Value = _editText.Text;
            }
        }

        private void OnEditTextLostFocus(object sender, RoutedEventArgs e)
        {
            if (IsEditing)
            {
                CommitEditing();
            }
        }

        private void OnEditTextKeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Enter:
                    if (!Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
                        .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
                    {
                        CommitEditing();
                        e.Handled = true;
                    }
                    break;
                case VirtualKey.Escape:
                    CancelEditing();
                    e.Handled = true;
                    break;
            }
        }

        #endregion

        #region Cell Operations

        /// <summary>
        /// Začne editáciu bunky
        /// </summary>
        public void StartEditing()
        {
            if (CellViewModel?.IsReadOnly == true || IsEditing) return;

            IsEditing = true;
            CellViewModel?.StartEditing();

            EditingStarted?.Invoke(this, CellViewModel!);

            // Focus the edit control
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (_editText != null)
                {
                    _editText.Focus(FocusState.Programmatic);
                    _editText.SelectAll();
                }
            });
        }

        /// <summary>
        /// Potvrdí editáciu
        /// </summary>
        public void CommitEditing()
        {
            if (!IsEditing || CellViewModel == null) return;

            // Sync final value from TextBox
            if (_editText != null)
            {
                CellViewModel.Value = _editText.Text;
            }

            CellViewModel.CommitChanges();
            IsEditing = false;

            EditingCompleted?.Invoke(this, CellViewModel);

            // Return focus to the cell
            this.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Zruší editáciu
        /// </summary>
        public void CancelEditing()
        {
            if (!IsEditing || CellViewModel == null) return;

            CellViewModel.CancelEditing();
            IsEditing = false;

            // Restore display value
            UpdateDisplayValue();

            EditingCancelled?.Invoke(this, CellViewModel);

            // Return focus to the cell
            this.Focus(FocusState.Programmatic);
        }

        #endregion

        #region Static Event Handlers

        private static void OnCellViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EditableCell cell)
            {
                // Unsubscribe from old ViewModel
                if (e.OldValue is CellViewModel oldVM)
                {
                    oldVM.PropertyChanged -= cell.OnCellViewModelPropertyChanged;
                }

                // Subscribe to new ViewModel
                if (e.NewValue is CellViewModel newVM)
                {
                    newVM.PropertyChanged += cell.OnCellViewModelPropertyChanged;
                }

                cell.UpdateDisplayValue();
                cell.UpdateValidationState();
                cell.UpdateVisualState();
            }
        }

        private static void OnIsEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EditableCell cell)
            {
                cell.UpdateVisualState();
            }
        }

        private static void OnValidationErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EditableCell cell)
            {
                cell.UpdateValidationState();
            }
        }

        #endregion

        #region Private Methods

        private void OnCellViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!_isTemplateApplied) return;

            switch (e.PropertyName)
            {
                case nameof(CellViewModel.DisplayValue):
                case nameof(CellViewModel.Value):
                    UpdateDisplayValue();
                    break;
                case nameof(CellViewModel.HasValidationErrors):
                    UpdateValidationState();
                    break;
                case nameof(CellViewModel.IsEditing):
                    IsEditing = CellViewModel?.IsEditing ?? false;
                    break;
                case nameof(CellViewModel.HasFocus):
                    UpdateVisualState();
                    break;
            }
        }

        private void UpdateDisplayValue()
        {
            if (!_isTemplateApplied) return;

            var displayValue = CellViewModel?.DisplayValue ?? "";

            if (_displayText != null)
            {
                _displayText.Text = displayValue;
            }

            if (_editText != null && !IsEditing)
            {
                _editText.Text = displayValue;
            }
        }

        private void UpdateVisualState()
        {
            if (!_isTemplateApplied) return;

            // OPRAVA 3: Proper Visual States
            var stateName = IsEditing ? "Editing" : "Normal";
            VisualStateManager.GoToState(this, stateName, true);

            var focusState = CellViewModel?.HasFocus == true ? "Focused" : "Unfocused";
            VisualStateManager.GoToState(this, focusState, true);

            var readOnlyState = CellViewModel?.IsReadOnly == true ? "ReadOnly" : "Editable";
            VisualStateManager.GoToState(this, readOnlyState, true);
        }

        private void UpdateValidationState()
        {
            if (!_isTemplateApplied) return;

            var hasErrors = CellViewModel?.HasValidationErrors == true;
            HasValidationError = hasErrors;

            // OPRAVA 3: Proper validation visual feedback
            var validationState = hasErrors ? "ValidationError" : "Valid";
            VisualStateManager.GoToState(this, validationState, true);

            // Update tooltip with validation errors
            if (hasErrors && !string.IsNullOrEmpty(CellViewModel?.ValidationErrorsText))
            {
                ToolTipService.SetToolTip(this, CellViewModel.ValidationErrorsText);
            }
            else
            {
                ToolTipService.SetToolTip(this, null);
            }

            // Update validation border
            if (_validationBorder != null)
            {
                _validationBorder.BorderBrush = hasErrors
                    ? new SolidColorBrush(Microsoft.UI.Colors.Red)
                    : new SolidColorBrush(Microsoft.UI.Colors.Transparent);

                _validationBorder.BorderThickness = hasErrors
                    ? new Thickness(2)
                    : new Thickness(0);
            }
        }

        private static bool IsPrintableKey(VirtualKey key)
        {
            return (key >= VirtualKey.A && key <= VirtualKey.Z) ||
                   (key >= VirtualKey.Number0 && key <= VirtualKey.Number9) ||
                   (key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9) ||
                   key == VirtualKey.Space;
        }

        #endregion
    }
}