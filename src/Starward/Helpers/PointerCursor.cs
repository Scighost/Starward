using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using System.Runtime.CompilerServices;

namespace Starward.Helpers;

public class PointerCursor : DependencyObject
{
    public static readonly DependencyProperty CursorShapeProperty =
        DependencyProperty.RegisterAttached("CursorShape", typeof(InputSystemCursorShape), typeof(PointerCursor), new PropertyMetadata(default));

    public static void SetCursorShape(UIElement element, InputSystemCursorShape value)
    {
        SetProtectedCursor(element, InputSystemCursor.Create(value));
        element.SetValue(CursorShapeProperty, value);
    }

    public static InputSystemCursorShape GetCursorShape(UIElement element)
    {
        return element.GetValue(CursorShapeProperty) switch
        {
            InputSystemCursorShape e => e,
            _ => InputSystemCursorShape.Arrow,
        };
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ProtectedCursor")]
    static extern void SetProtectedCursor(UIElement element, InputCursor cursor);
}
