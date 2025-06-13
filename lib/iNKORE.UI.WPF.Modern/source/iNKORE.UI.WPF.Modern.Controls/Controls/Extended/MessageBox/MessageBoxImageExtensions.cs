using System;
using System.Media;
using System.Windows;
using iNKORE.UI.WPF.Modern.Common.IconKeys;

namespace iNKORE.UI.WPF.Modern.Extensions
{
    internal static class MessageBoxImageExtensions
    {
        public static FontIconData ToSymbol(this MessageBoxImage image)
        {
            return image switch
            {
                MessageBoxImage.Error => SegoeFluentIcons.ErrorBadge,
                MessageBoxImage.Information => SegoeFluentIcons.Info,
                MessageBoxImage.Warning => SegoeFluentIcons.Warning,
                MessageBoxImage.Question => SegoeFluentIcons.Unknown,
                MessageBoxImage.None => new FontIconData(char.ConvertFromUtf32(0x2007)),
                _ => new FontIconData(char.ConvertFromUtf32(0x2007)),
            };
        }


        public static SystemSound ToAlertSound(this MessageBoxImage image)
        {
            return image switch
            {
                MessageBoxImage.Error => SystemSounds.Hand,
                MessageBoxImage.Information => SystemSounds.Asterisk,
                MessageBoxImage.Warning => SystemSounds.Exclamation,
                MessageBoxImage.Question => SystemSounds.Question,
                MessageBoxImage.None => null,
                _ => null,
            };

        }
    }
}
