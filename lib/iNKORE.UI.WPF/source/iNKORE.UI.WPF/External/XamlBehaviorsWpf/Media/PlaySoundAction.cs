// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors.Media
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using Microsoft.Xaml.Behaviors;

    /// <summary>
    /// An action that will play a sound to completion.
    /// </summary>
    /// <remarks>
    /// This action is intended for use with short sound effects that don't need to be stopped or controlled. If you're trying 
    /// to create a music player or game, it may not meet your needs.
    /// </remarks>
    public class PlaySoundAction : TriggerAction<DependencyObject>
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(Uri), typeof(PlaySoundAction), null);
        public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register("Volume", typeof(double), typeof(PlaySoundAction), new PropertyMetadata(0.5));

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaySoundAction"/> class.
        /// </summary>
        public PlaySoundAction()
        {
        }

        /// <summary>
        /// A Uri defining the location of the sound file. This is used to set the source property of the MediaElement. This is a dependency property.
        /// </summary>
        /// <remarks>
        /// The sound can be any file format supported by MediaElement. In the case of a video, it will play only the
        /// audio portion.
        /// </remarks>
        public Uri Source
        {
            get { return (Uri)this.GetValue(SourceProperty); }
            set { this.SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// Control the volume of the sound. This is used to set the Volume property of the MediaElement. This is a dependency property.
        /// </summary>
        public double Volume
        {
            get { return (double)this.GetValue(VolumeProperty); }
            set { this.SetValue(VolumeProperty, value); }
        }

        /// <summary>
        /// When the action is invoked, this method is used to customize the dynamically created MediaElement.
        /// </summary>
        /// <remarks>
        /// This method may be useful for Action authors who wish to extend PlaySoundAction. If you want to control the 
        /// MediaElement Balance property, you could inherit from PlaySoundAction and override this method.
        /// </remarks>
        /// <param name="mediaElement"></param>
        protected virtual void SetMediaElementProperties(MediaElement mediaElement)
        {
            if (mediaElement != null)
            {
                mediaElement.Source = this.Source;
                mediaElement.Volume = this.Volume;
            }
        }

        /// <summary>
        /// This method is called when some criteria are met and the action should be invoked. 
        /// </summary>
        /// <remarks>
        /// Each invocation of the Action plays a new sound. Although the implementation is subject-to-change, the caller should 
        /// anticipate that this will create a new MediaElement that will be cleaned up when the sound completes or if the media 
        /// fails to play.
        /// </remarks>
        /// <param name="parameter"></param>
        protected override void Invoke(object parameter)
        {
            if (this.Source == null || this.AssociatedObject == null)
            {
                return;
            }

            Popup popup = new Popup();
            MediaElement mediaElement = new MediaElement();
            popup.Child = mediaElement;
            // It is legal (although not advisable) to provide a video file. By setting visibility to collapsed, only the sound track should play.
            mediaElement.Visibility = Visibility.Collapsed;

            this.SetMediaElementProperties(mediaElement);

            // Setup delegates that will free the MediaElement upon completion or failure 
            mediaElement.MediaEnded += delegate
            {
                popup.Child = null;
                popup.IsOpen = false;
            };

            mediaElement.MediaFailed += delegate
            {
                popup.Child = null;
                popup.IsOpen = false;
            };

            popup.IsOpen = true;
        }
    }
}
