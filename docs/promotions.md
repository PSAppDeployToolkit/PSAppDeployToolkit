
# Spread the Word!

Ho do you like this project so far? If this project is useful to you, please consider giving it a star on GitHub, sharing it with others and add a banner with a link to this project in your README.

We'd really appreciate it if you could help us spread the word about this project. Well currently, you can kindly add a badge and banner to your README file and the About section of your application.

> [!NOTE]
>
> This article will be soon moved to the official documentation site (docs.inkore.net, currently under construction). Please check the official documentation site for the latest information.

## Banners

We've prepared a rather beautiful banner for you, which goes along with the Fluent Design System, and is simple, clean and modern.

<a href="https://docs.inkore.net/ui-wpf-modern/introduction">
  <img src="https://github.com/iNKORE-NET/UI.WPF.Modern/blob/main/assets/images/banners/UI.WPF.Modern_Main_1280w.png?raw=true" alt="iNKORE.UI.WPF.Modern">
</a>
We have prepared two sizes for you to choose from:

- **2560x1280**: https://github.com/iNKORE-NET/UI.WPF.Modern/blob/main/assets/images/banners/UI.WPF.Modern_Main_2560w.png?raw=true

- **1280x640**: https://github.com/iNKORE-NET/UI.WPF.Modern/blob/main/assets/images/banners/UI.WPF.Modern_Main_1280w.png?raw=true

To add the banner to your README, copy the following markdown code. If you want a higher resolution banner, you can change the `1280w` to `2560w` in the URL.

```markdown
<a href="https://docs.inkore.net/ui-wpf-modern/introduction">
  <img src="https://github.com/iNKORE-NET/UI.WPF.Modern/blob/main/assets/images/banners/UI.WPF.Modern_Main_1280w.png?raw=true" alt="iNKORE.UI.WPF.Modern">
</a>
```

You can also add this banner to the About section of your application. This banner is built-in with the library set, so you can use it directly in your application. Additonally, this banner will be updated with the library, so you don't have to worry about updating it.

Use the `ThemeManager.BannerUri_1280w` field to get the banner URI, and you can do something in XAML. Remember that there's only one size (1280w) available in the library due to bundle size concerns.

```xml
<Image x:Name="headerImage" Stretch="Uniform">
    <Image.Source>
        <BitmapImage UriSource="{x:Static ui:ThemeManager.BannerUri_1280w}"/>
    </Image.Source>
</Image>
```

You may also add a Click event to the image to open the project page if you like, which helps users to find the project page easily.

Posting the banner on social media is **always allowed and encouraged**. You can also use the banner in your blog posts, videos, and other content.

## Badges

Tiny badges are also a great way to show your support for this project if the banners are too large for you. All badges comes in a SVG and built-in control. There are two types of badges available:

### **Button** Style

Badges meet the Fluent Design System. It looks just like a regular button, which helps it playing with the other controls in your application.

<a href="https://github.com/iNKORE-NET/UI.WPF.Modern">
  <img src="https://github.com/iNKORE-NET/UI.WPF.Modern/blob/main/assets/images/badges/UI.WPF.Modern_Main_Button.svg?raw=true" alt="iNKORE.UI.WPF.Modern">
</a>

To add the button badge to your README, use the following markdown code.

```markdown
<a href="https://github.com/iNKORE-NET/UI.WPF.Modern">
  <img src="https://github.com/iNKORE-NET/UI.WPF.Modern/blob/main/assets/images/badges/UI.WPF.Modern_Main_Button.svg?raw=true" alt="iNKORE.UI.WPF.Modern">
</a>
```

It can also be used in your application, which brings MORE features than the image, like hover, click effects and theme awareness (light and dark scheme). We strongly recommend using this badge in your application to go along with the other controls.

```xml
<ui:ProjectBadge Style="{DynamicResource {x:Static ui:ThemeKeys.ProjectBadgeButtonStyleKey}}"/>
```

The ProjectBadge control is using the button style by default, so you can remove the `Style` attribute if you want, the badge will stay the same.

```xml
<ui:ProjectBadge/>
```

### **Shield** Style

Badges are quite popular in the open-source community. The shield style is a common way to show your support for a project. It's simple, clean and modern. There are usually a few badges in the README of a project stacked together.

<a href="https://github.com/iNKORE-NET/UI.WPF.Modern">
  <img src="https://github.com/iNKORE-NET/UI.WPF.Modern/blob/main/assets/images/badges/UI.WPF.Modern_Main_Shield.svg?raw=true" alt="iNKORE.UI.WPF.Modern">
</a>

To add the shield badge to your README, use the following markdown code.

```markdown
<a href="https://github.com/iNKORE-NET/UI.WPF.Modern">
  <img src="https://github.com/iNKORE-NET/UI.WPF.Modern/blob/main/assets/images/badges/UI.WPF.Modern_Main_Shield.svg?raw=true" alt="iNKORE.UI.WPF.Modern">
</a>
```

Using the shield badge in your application is also possible, even if it doesn't meet the Fluent Design standards, you may want it anyway. To add the shield badge to your application, an explicit style key is required.

```xml
<ui:ProjectBadge Style="{DynamicResource {x:Static ui:ThemeKeys.ProjectBadgeShieldStyleKey}}"/>
```

## Conclusion

All the badges and banners are updated with the library and the repository, so you don't have to worry about updating them if you are doing it as above. In some cases, you may also want to download the badges and banners to your local machine to use them. This is also possible, and you can find them in the `assets/images/badges` and `assets/images/banners` folders in the repository. But we don't recommend this way, as the badges and banners may be updated frequently.

Thank you for your support! If you love this, you can also consider sponsoring us, as it helps us to keep the project alive and make it better.