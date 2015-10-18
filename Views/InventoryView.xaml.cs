using Slate_EK.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Slate_EK.Views
{
    /// <summary>
    /// Interaction logic for InventoryView.xaml
    /// </summary>
    public partial class InventoryView
    {
        private InventoryViewModel ViewModel
        {
            get
            {
                if (DataContext != null && DataContext is InventoryViewModel)
                    return (InventoryViewModel)DataContext;
                return null;
            }
            set
            {
                DataContext = value;
            }
        }

        public InventoryView() : this(Properties.Settings.Default.DefaultInventoryPath) { }

        public InventoryView(string inventoryPath)
        {
            //RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);

            InitializeComponent();

            ViewModel = new InventoryViewModel(inventoryPath);

            ViewModel.RegisterCloseAction(Close);
            ViewModel.ShortcutPressedCtrlQ += () => QueryTextField.Focus();

            InventoryItemsControl.ItemsSource = ViewModel.FastenerList; 
        }

        private void UIElement_OnGotFocus(object sender, RoutedEventArgs e)
        {
            (e.OriginalSource as TextBox)?.SelectAll();
        }

        #region // Click / drag / selection handling
        
        protected bool  IsMouseDown;
        protected Point MouseDownPos;

        protected int SelectDownIndex   = -1;
        protected int SelectUpIndex     = -1;

        protected List<FastenerControl>     PassedOver;
        protected List<FastenerControl>     PreviouslySelected;

        protected bool HasFastenerSelected
        {
            get
            {
                foreach (FastenerControl item in InventoryItemsControl.Items)
                {
                    if (item.IsSelected)
                        return true;
                }

                return false;
            }
        }
        protected int SelectedFastenersCount
        {
            get
            {
                int count = 0;
                foreach (FastenerControl item in InventoryItemsControl.Items)
                {
                    if (item.IsSelected) count++;
                }

                return count;
            }
        }
        protected int FirstSelectedIndex
        {
            get
            {
                if (!HasFastenerSelected) return -1;

                return InventoryItemsControl.Items.IndexOf
                (
                    PreviouslySelected.First()
                );
            }
        }
        protected int LastSelectedIndex
        {
            get
            {
                if (!HasFastenerSelected) return -1;

                return InventoryItemsControl.Items.IndexOf
                (
                    PreviouslySelected.Last()
                );
            }
        }

        private void InventoryBox_LeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            InventoryBox.CaptureMouse();

            IsMouseDown             = true;
            MouseDownPos            = e.GetPosition(InventoryBox);
            PreviouslySelected      = new List<FastenerControl>();
            PassedOver              = new List<FastenerControl>();

            foreach (FastenerControl item in InventoryItemsControl.Items)
            {
                if (item.IsSelected)
                    PreviouslySelected.Add(item);
            }

            Canvas.SetLeft(SelectionBox, MouseDownPos.X);
            Canvas.SetTop(SelectionBox, MouseDownPos.Y);

            SelectionBox.Width  = 0;
            SelectionBox.Height = 0;

            SelectionBox.Visibility = Visibility.Visible;

            if (e.OriginalSource is FrameworkElement)
            {
                FrameworkElement origin = GetRootFastenerElement(e.OriginalSource as FrameworkElement);

                if (origin != null)
                {
                    SelectDownIndex = InventoryItemsControl.Items.IndexOf(origin.DataContext);
                }
            }

            HandleSelections(true);

            //
            // Handle double click
            if (e.ClickCount == 2 && SelectedFastenersCount == 1)
            {
                InventoryBox.ReleaseMouseCapture();
                IsMouseDown = false;
                SelectionBox.Visibility = Visibility.Collapsed;

                var o = Mouse.DirectlyOver;
                Extender.Debugging.Debug.WriteMessage(o.ToString());
            }
        }

        private void InventoryBox_LeftMouseUp(object sender, MouseButtonEventArgs e)
        {
            InventoryBox.ReleaseMouseCapture();

            if (IsMouseDown)
            {
                IsMouseDown = false;
                SelectionBox.Visibility = Visibility.Collapsed;

                Point mouseUpPos = e.GetPosition(InventoryBox);

                if (e.OriginalSource is FrameworkElement)
                {
                    FrameworkElement origin = GetRootFastenerElement(e.OriginalSource as FrameworkElement);

                    if (origin != null)
                        SelectUpIndex = InventoryItemsControl.Items.IndexOf(origin.DataContext);
                }

                if (SelectDownIndex >= 0 &&
                    HasFastenerSelected &&
                    (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) &&
                    mouseUpPos.Y.Equals(MouseDownPos.Y))
                {
                    // Single click with shift held
                    if (SelectDownIndex > LastSelectedIndex)
                    {
                        for (int i = LastSelectedIndex; i < SelectDownIndex; i++)
                        {
                            (InventoryItemsControl.Items[i] as FastenerControl)?.SelectCommand.Execute(null);
                        }
                    }
                    else if (SelectDownIndex < FirstSelectedIndex)
                    {
                        for (int i = SelectDownIndex; i < FirstSelectedIndex; i++)
                        {
                            (InventoryItemsControl.Items[i] as FastenerControl)?.SelectCommand.Execute(null);
                        }
                    }
                }

                SelectDownIndex = -1;
                SelectUpIndex   = -1;
            }
        }

        private void InventoryBox_RightMouseUp(object sender, MouseButtonEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is FastenerControl && (SelectedFastenersCount <= 1))
            {
                ClearFastenersSelection();
                (((FrameworkElement)e.OriginalSource).DataContext as FastenerControl)?.SelectCommand.Execute(null);
            }
        }

        private void InventoryBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseDown)
            {
                Point curPos = e.GetPosition(InventoryBox);

                //
                // resize the selection box
                if (MouseDownPos.X < curPos.X)
                {
                    Canvas.SetLeft(SelectionBox, MouseDownPos.X);
                    SelectionBox.Width = curPos.X - MouseDownPos.X;
                }
                else
                {
                    Canvas.SetLeft(SelectionBox, curPos.X);
                    SelectionBox.Width = MouseDownPos.X - curPos.X;
                }

                if (MouseDownPos.Y < curPos.Y)
                {
                    Canvas.SetTop(SelectionBox, MouseDownPos.Y);
                    SelectionBox.Height = curPos.Y - MouseDownPos.Y;
                }
                else
                {
                    Canvas.SetTop(SelectionBox, curPos.Y);
                    SelectionBox.Height = MouseDownPos.Y - curPos.Y;
                }

                HandleSelections(false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source">The source element whose tree we're searching.</param>
        /// <returns>
        /// Returns the highest level element in the tree with a FastenerControl as its DataContext.
        /// Return null if 'source' doesn't have a FastenerControl as its DataContext.
        /// </returns>
        private FrameworkElement GetRootFastenerElement(FrameworkElement source)
        {
            if (source.DataContext != null && source.DataContext is FastenerControl)
            {
                FrameworkElement root = source;

                while (root.Parent != null &&
                      ((FrameworkElement)root.Parent).DataContext != null &&
                      ((FrameworkElement)root.Parent).DataContext is FastenerControl)
                {
                    root = (FrameworkElement)root.Parent;
                }

                return root;
            }

            return null;
        }

        private void HandleSelections(bool firstClick) 
        {
            bool ctrlDown   = false;
            bool shiftDown  = false;

            //
            // Check modifier keys
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                // Handle CTRL key down
                ctrlDown = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                // Handle Shift key down
                shiftDown = true;
            }
            else
            {
                // Handle no modifiers
                ClearFastenersSelection();
            }

            //
            // Get selection box bounds
            Rect selectionBounds;
            if (firstClick)
            {
                selectionBounds = new Rect(MouseDownPos, new Size(2, 2));
                // The rectangle used for selection hasn't actually been drawn yet, 
                // so we need to fake it.
            }
            else
            {
                selectionBounds = new Rect
                (
                    SelectionBox.TranslatePoint(new Point(0, 0), InventoryBox),
                    new Size(SelectionBox.ActualWidth, SelectionBox.ActualHeight)
                );
            }

            //
            // Make a list of the elements in InventoryItemsControl so we can hit test them
            FrameworkElement[] inventoryElemeents = new FrameworkElement[InventoryItemsControl.Items.Count];
            for (int i = 0; i < inventoryElemeents.Length; i++)
            {
                DependencyObject container = InventoryItemsControl.ItemContainerGenerator.ContainerFromIndex(i);
                if (container is FrameworkElement)
                    inventoryElemeents[i] = container as FrameworkElement;
            }

            //
            // Select and/or deselect 
            foreach (FrameworkElement fastenerElement in inventoryElemeents)
            {
                if ((FastenerControl)fastenerElement?.DataContext == null)
                    continue;

                FastenerControl fastener = fastenerElement.DataContext as FastenerControl;

                Rect hitBox = new Rect
                (
                    fastenerElement.TranslatePoint(new Point(0, 0), InventoryBox),
                    new Size(fastenerElement.ActualWidth, fastenerElement.ActualHeight)
                );

                bool inSelection = selectionBounds.IntersectsWith(hitBox);

                if (ctrlDown)
                {
                    if (inSelection && !PreviouslySelected.Contains(fastener))
                    {
                        fastener.SelectCommand.Execute(null);
                    }
                    else if (inSelection && PreviouslySelected.Contains(fastener))
                    {
                        fastener.DeselectCommand.Execute(null);
                    }
                    else if (!inSelection && PreviouslySelected.Contains(fastener))
                    {
                        fastener.SelectCommand.Execute(null);
                    }
                    else
                    {
                        fastener.DeselectCommand.Execute(null);
                    }
                }
                else if (shiftDown)
                {
                    if (inSelection)
                    {
                        fastener.SelectCommand.Execute(null);
                        PassedOver.Add(fastener);
                    } 
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse // for clarity
                    else if (!inSelection && PassedOver.Contains(fastener))
                    {
                        fastener.DeselectCommand.Execute(null);
                    }
                }
                else
                {
                    if (inSelection && 
                        !(!HasFastenerSelected && PreviouslySelected.Count <= 1 && PreviouslySelected.Contains(fastener)))
                    {
                        fastener.SelectCommand.Execute(null);
                    }
                    else fastener.DeselectCommand.Execute(null);
                }
            }
        }

        private void ClearFastenersSelection()
        {
            foreach (var item in InventoryItemsControl.Items)
            {
                (item as FastenerControl)?.DeselectCommand.Execute(null);
            }
        }

        #endregion

        #region // Settings.settings aliases

        public bool Debug => Properties.Settings.Default.Debug;

        #endregion

    }
}
