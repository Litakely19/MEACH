using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SchoolManagement.WPF.Behaviors;

/// <summary>
/// Makes <see cref="PasswordBox.Password"/> bindable so credential screens keep
/// their logic in the view model instead of in code behind.
/// </summary>
public static class PasswordBoxBehavior
{
    public static readonly DependencyProperty BoundPasswordProperty = DependencyProperty.RegisterAttached(
        "BoundPassword",
        typeof(string),
        typeof(PasswordBoxBehavior),
        new FrameworkPropertyMetadata(
            null,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnBoundPasswordChanged)
        {
            DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });

    private static readonly DependencyProperty IsAttachedProperty = DependencyProperty.RegisterAttached(
        "IsAttached",
        typeof(bool),
        typeof(PasswordBoxBehavior),
        new PropertyMetadata(false));

    private static readonly DependencyProperty IsUpdatingProperty = DependencyProperty.RegisterAttached(
        "IsUpdating",
        typeof(bool),
        typeof(PasswordBoxBehavior),
        new PropertyMetadata(false));

    static PasswordBoxBehavior()
    {
        // BoundPassword often starts empty on both the box and the view model.
        // WPF then skips the property-changed callback, so PasswordChanged was
        // never hooked and Sign in always received an empty password.
        EventManager.RegisterClassHandler(
            typeof(PasswordBox),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnPasswordBoxLoaded));
    }

    public static string GetBoundPassword(DependencyObject element) =>
        (string?)element.GetValue(BoundPasswordProperty) ?? string.Empty;

    public static void SetBoundPassword(DependencyObject element, string? value) =>
        element.SetValue(BoundPasswordProperty, value ?? string.Empty);

    private static void OnPasswordBoxLoaded(object sender, RoutedEventArgs args)
    {
        if (sender is PasswordBox passwordBox)
        {
            TryAttach(passwordBox);
        }
    }

    private static void OnBoundPasswordChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not PasswordBox passwordBox)
        {
            return;
        }

        TryAttach(passwordBox);

        // Writing Password here would raise PasswordChanged and push the caret to
        // the end while the operator is typing, so only external resets are applied.
        if ((bool)passwordBox.GetValue(IsUpdatingProperty))
        {
            return;
        }

        var newPassword = args.NewValue as string ?? string.Empty;

        if (!string.Equals(passwordBox.Password, newPassword, StringComparison.Ordinal))
        {
            passwordBox.Password = newPassword;
        }
    }

    private static void TryAttach(PasswordBox passwordBox)
    {
        if ((bool)passwordBox.GetValue(IsAttachedProperty))
        {
            return;
        }

        if (BindingOperations.GetBinding(passwordBox, BoundPasswordProperty) is null
            && BindingOperations.GetBindingExpression(passwordBox, BoundPasswordProperty) is null)
        {
            return;
        }

        passwordBox.SetValue(IsAttachedProperty, true);
        passwordBox.PasswordChanged += OnPasswordChanged;
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs args)
    {
        var passwordBox = (PasswordBox)sender;

        passwordBox.SetValue(IsUpdatingProperty, true);
        SetBoundPassword(passwordBox, passwordBox.Password);
        passwordBox.SetValue(IsUpdatingProperty, false);
    }
}
