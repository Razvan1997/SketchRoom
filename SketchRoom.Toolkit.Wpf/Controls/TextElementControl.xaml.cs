using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WhiteBoard.Core.Events;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Tools;

namespace SketchRoom.Toolkit.Wpf.Controls
{
    /// <summary>
    /// Interaction logic for TextElementControl.xaml
    /// </summary>
    public partial class TextElementControl : UserControl, ITextInteractiveShape
    {
        private bool _isDragging = false;
        private Point _dragStart;

        private bool _isRotating = false;
        private Point _rotateStart;
        public RotateTransform RotateTransform { get; } = new RotateTransform();
        public TranslateTransform TranslateTransform { get; } = new TranslateTransform();
        public ScaleTransform ScaleTransform { get; } = new ScaleTransform();
        public TransformGroup TransformGroup { get; } = new TransformGroup();
        public TextBox EditableText => EditableTextBox;

        private IDrawingPreferencesService? _preferences;
        public TextElementControl()
        {
            InitializeComponent();

            // Compose transforms
            TransformGroup.Children.Add(ScaleTransform);
            TransformGroup.Children.Add(RotateTransform);
            TransformGroup.Children.Add(TranslateTransform);
            this.RenderTransform = TransformGroup;
            this.RenderTransformOrigin = new Point(0.5, 0.5);

            // Rotation
            RotateIcon.PreviewMouseLeftButtonDown += RotateIcon_PreviewMouseLeftButtonDown;
            RotateIcon.PreviewMouseLeftButtonUp += RotateIcon_PreviewMouseLeftButtonUp;
            RotateIcon.PreviewMouseMove += RotateIcon_PreviewMouseMove;

            // Optional: text focus
            EditableText.GotFocus += (s, e) => IsTextEditing = true;
            EditableText.LostFocus += (s, e) => IsTextEditing = false;

            EditableText.PreviewMouseDown += (s, e) =>
            {
                EditableText.BorderThickness = new Thickness(2);
                EditableText.BorderBrush = Brushes.White;

                EditableText.Focus();
                EditableText.SelectAll();
                RotateIcon.Visibility = Visibility.Visible;
            };
        }

        // ITextInteractiveShape implementation
        public UIElement Visual => this;
        public bool EnableConnectors { get; set; } = false;
        public event MouseButtonEventHandler? ShapeClicked;
        public event EventHandler<ConnectionPointEventArgs>? ConnectionPointClicked;
        public event EventHandler? ConnectionRequested;
        public event EventHandler<ConnectionPointEventArgs>? ConnectionPointTargetClicked;

        public bool IsTextEditing { get; private set; }

        public string Text
        {
            get => EditableText.Text;
            set => EditableText.Text = value;
        }

        public void SetShape(ShapeType shape)
        {
            if (shape == ShapeType.TextInput)
            {
                EditableText.FontFamily = new FontFamily("Segoe UI");
                EditableText.FontSize = 14;
                EditableText.Foreground = Brushes.Black;
                EditableText.Tag = "text";
            }
        }

        public void SetPosition(Point position)
        {
            Canvas.SetLeft(this, position.X);
            Canvas.SetTop(this, position.Y);
        }

        public void Select()
        {
            EditableText.Focus();
            EditableText.SelectAll();
        }

        public void Deselect()
        {
            EditableText.BorderThickness = new Thickness(0);
            EditableText.BorderBrush = Brushes.Transparent;
            RotateIcon.Visibility = Visibility.Collapsed;
        }

        public void FocusText()
        {
            ApplyCurrentPreferences();
            EditableText.Focus();
            EditableText.SelectAll();
        }

        public void SetRotation(double angle)
        {
            RotateTransform.Angle = angle;
        }

        // Drag to move
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsTextEditing) return; // 🔒 Nu porni drag dacă editezi textul

            _isDragging = true;
            _dragStart = e.GetPosition(Parent as UIElement);
            CaptureMouse();
            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if(!_isDragging || IsTextEditing) return;

            var canvas = Parent as Canvas;
            if (canvas == null) return;

            Point currentPos = e.GetPosition(canvas);
            double offsetX = currentPos.X - _dragStart.X;
            double offsetY = currentPos.Y - _dragStart.Y;

            double left = Canvas.GetLeft(this) + offsetX;
            double top = Canvas.GetTop(this) + offsetY;

            Canvas.SetLeft(this, left);
            Canvas.SetTop(this, top);

            _dragStart = currentPos;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging || IsTextEditing) return;

            _isDragging = false;
            ReleaseMouseCapture();
        }

        // Rotation events
        private void RotateIcon_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var canvas = VisualTreeHelper.GetParent(this) as Canvas;
            var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();
            var toolManager = tabService.GetCurrentToolManager();

            if (canvas != null && toolManager.GetToolByName("RotateTool") is RotateTool rt)
            {
                _isRotating = true;
                _rotateStart = e.GetPosition(canvas);
                rt.StartRotation(this, _rotateStart);
                toolManager.SetActive("RotateTool");

                RotateIcon.CaptureMouse(); // ✅ capturează mouse-ul
                e.Handled = true;
            }
        }

        private void RotateIcon_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isRotating) return;

            var canvas = VisualTreeHelper.GetParent(this) as Canvas;
            var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();
            var toolManager = tabService.GetCurrentToolManager();

            if (canvas != null && toolManager.ActiveTool is RotateTool rt)
            {
                Point pos = e.GetPosition(canvas);
                rt.OnMouseMove(pos);
            }
        }

        private void RotateIcon_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var canvas = VisualTreeHelper.GetParent(this) as Canvas;
            var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();
            var toolManager = tabService.GetCurrentToolManager();

            if (canvas != null && _isRotating && toolManager.ActiveTool is RotateTool rt)
            {
                rt.OnMouseUp(e.GetPosition(canvas));
                toolManager.SetActive("TextEdit");
                _isRotating = false;

                RotateIcon.ReleaseMouseCapture(); // ✅ eliberează captura
                e.Handled = true;
            }
        }

        // Auto resize to content (optional)
        private void EditableText_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSizeToFitContent();
        }

        public void SetPreferences()
        {
            _preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            ApplyCurrentPreferences();
        }

        private void ApplyCurrentPreferences()
        {
            if (_preferences == null) return;

            EditableText.FontSize = _preferences.FontSize;
            EditableText.FontWeight = _preferences.FontWeight;
            EditableText.Foreground = _preferences.SelectedColor;

            UpdateSizeToFitContent();
        }

        private void UpdateSizeToFitContent()
        {
            if (string.IsNullOrEmpty(EditableText.Text))
                return;

            var typeface = new Typeface(
                EditableText.FontFamily,
                EditableText.FontStyle,
                EditableText.FontWeight,
                EditableText.FontStretch);

            var formattedText = new FormattedText(
                EditableText.Text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                EditableText.FontSize,
                EditableText.Foreground,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            // Width: text width + padding
            this.Width = formattedText.Width + 20;

            // Height: text height + padding
            this.Height = formattedText.Height + 20;
        }
    }
}
