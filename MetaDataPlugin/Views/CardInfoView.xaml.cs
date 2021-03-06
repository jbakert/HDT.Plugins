﻿using HDT.Plugins.Custom.ViewModels;
using Hearthstone_Deck_Tracker.API;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HDT.Plugins.Custom.Controls
{
    /// <summary>
    /// Interaction logic for CardInfoView.xaml
    /// </summary>
    public partial class CardInfoView : UserControl
    {
        UIElement DockPanel => Core.OverlayCanvas?.FindName("BorderStackPanelPlayer") as UIElement;

        public static readonly DependencyProperty ControlRotateAngleProperty = DependencyProperty.Register("ControlRotateAngle", typeof(double), typeof(CardInfoView), new PropertyMetadata(0.0));

        public double ControlRotateAngle
        {
            get { return (double)GetValue(ControlRotateAngleProperty); }
            set { SetValue(ControlRotateAngleProperty, value); }
        }

        public static readonly DependencyProperty TextRotateAngleProperty = DependencyProperty.Register("TextRotateAngle", typeof(double), typeof(CardInfoView), new PropertyMetadata(0.0));

        public double TextRotateAngle
        {
            get { return (double)GetValue(TextRotateAngleProperty); }
            set { SetValue(TextRotateAngleProperty, value); }
        }


        private bool _verticalBars = false;
        public bool VerticalBars
        {
            get { return _verticalBars; }
            set
            {
                _verticalBars = value;

                ControlRotateAngle = value ? -90 : 0;
                TextRotateAngle = value ? 90 : 0;
            }
        }

        public CardInfoView()
        {
            InitializeComponent();

            DependencyPropertyDescriptor.FromProperty(Canvas.LeftProperty, typeof(Border)).AddValueChanged(DockPanel, UpdatePosition);
            DependencyPropertyDescriptor.FromProperty(Canvas.TopProperty, typeof(Border)).AddValueChanged(DockPanel, UpdatePosition);
            DependencyPropertyDescriptor.FromProperty(Canvas.ActualWidthProperty, typeof(Border)).AddValueChanged(DockPanel, UpdatePosition);


        }

        public void UpdatePosition(object sender, EventArgs e)
        {
            var panelLeft = DockPanel?.GetValue(Canvas.LeftProperty) as double?;
            var panelTop = DockPanel?.GetValue(Canvas.TopProperty) as double?;
            var panelWidth = DockPanel?.GetValue(Canvas.ActualWidthProperty) as double?;

            var pixelPadding = 20;

            if (panelLeft == null || panelTop == null)
            {
                panelLeft = Core.OverlayWindow.ActualWidth;
                panelTop = Core.OverlayWindow.Top + 20;
            }

            Canvas.SetLeft(this, (double)panelLeft - (this.ActualWidth + pixelPadding));
            Canvas.SetTop(this, (double)panelTop + 1);
        }

        public void Show()
        {
            this.Visibility = Visibility.Visible;
        }

        public void Hide()
        {

            this.Visibility = Visibility.Hidden;
        }

        public void Dispose()
        {
            DependencyPropertyDescriptor.FromProperty(Canvas.LeftProperty, typeof(Border)).RemoveValueChanged(DockPanel, UpdatePosition);
            DependencyPropertyDescriptor.FromProperty(Canvas.TopProperty, typeof(Border)).RemoveValueChanged(DockPanel, UpdatePosition);
            //  DependencyPropertyDescriptor.FromProperty(Canvas.ActualWidthProperty, typeof(Border)).RemoveValueChanged(DockPanel, UpdatePosition);
        }
    }
}
