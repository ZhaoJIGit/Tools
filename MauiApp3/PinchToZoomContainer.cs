using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp3
{
    public class PinchToZoomContainer : ContentView
    {
        //double currentScale = 0.5;
        //double startScale = 1;
        double xOffset = 0;
        double yOffset = 0;

        public static readonly BindableProperty CurrentScaleProperty = BindableProperty.Create(nameof(CurrentScale), typeof(double), typeof(PinchToZoomContainer), 1D);

        public double CurrentScale
        {
            get => (double)GetValue(CurrentScaleProperty);
            set => SetValue(CurrentScaleProperty, value);
        }


        public static readonly BindableProperty StartScaleProperty = BindableProperty.Create(nameof(StartScale), typeof(double), typeof(PinchToZoomContainer), 1D);

        public double StartScale
        {
            get => (double)GetValue(StartScaleProperty);
            set => SetValue(StartScaleProperty, value);
        }

        public PinchToZoomContainer()
        {
            PinchGestureRecognizer pinchGesture = new PinchGestureRecognizer();
            pinchGesture.PinchUpdated += OnPinchUpdated;
            GestureRecognizers.Add(pinchGesture);
        }

        void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            if (e.Status == GestureStatus.Started)
            {
                // Store the current scale factor applied to the wrapped user interface element,
                // and zero the components for the center point of the translate transform.
                StartScale = Content.Scale;
                Content.AnchorX = 0;
                Content.AnchorY = 0;
            }
            if (e.Status == GestureStatus.Running)
            {
                // Calculate the scale factor to be applied.
                CurrentScale += (e.Scale - 1) * StartScale;
                CurrentScale = Math.Max(1, CurrentScale);

                // The ScaleOrigin is in relative coordinates to the wrapped user interface element,
                // so get the X pixel coordinate.
                double renderedX = Content.X + xOffset;
                double deltaX = renderedX / Width;
                double deltaWidth = Width / (Content.Width * StartScale);
                double originX = (e.ScaleOrigin.X - deltaX) * deltaWidth;

                // The ScaleOrigin is in relative coordinates to the wrapped user interface element,
                // so get the Y pixel coordinate.
                double renderedY = Content.Y + yOffset;
                double deltaY = renderedY / Height;
                double deltaHeight = Height / (Content.Height * StartScale);
                double originY = (e.ScaleOrigin.Y - deltaY) * deltaHeight;

                // Calculate the transformed element pixel coordinates.
                double targetX = xOffset - (originX * Content.Width) * (CurrentScale - StartScale);
                double targetY = yOffset - (originY * Content.Height) * (CurrentScale - StartScale);

                // Apply translation based on the change in origin.
                Content.TranslationX = Math.Clamp(targetX, -Content.Width * (CurrentScale - 1), 0);
                Content.TranslationY = Math.Clamp(targetY, -Content.Height * (CurrentScale - 1), 0);

                // Apply scale factor
                Content.Scale = CurrentScale;
            }
            if (e.Status == GestureStatus.Completed)
            {
                // Store the translation delta's of the wrapped user interface element.
                xOffset = Content.TranslationX;
                yOffset = Content.TranslationY;
            }
        }
    }
}
