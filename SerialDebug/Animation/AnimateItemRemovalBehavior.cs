using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace SerialDebug.Animation
{
    public class AnimateItemRemovalBehavior
    {
        public static readonly DependencyProperty StoryboardProperty =
            DependencyProperty.RegisterAttached(
                "Storyboard",
                typeof(Storyboard),
                typeof(AnimateItemRemovalBehavior),
                null);

        public static Storyboard GetStoryboard(DependencyObject o)
        {
            return o.GetValue(StoryboardProperty) as Storyboard;
        }

        public static void SetStoryboard(DependencyObject o, Storyboard value)
        {
            o.SetValue(StoryboardProperty, value);
        }

        public static readonly DependencyProperty PerformRemovalProperty =
            DependencyProperty.RegisterAttached(
                "PerformRemoval",
                typeof(ICommand),
                typeof(AnimateItemRemovalBehavior),
                null);

        public static ICommand GetPerformRemoval(DependencyObject o)
        {
            return o.GetValue(PerformRemovalProperty) as ICommand;
        }

        public static void SetPerformRemoval(DependencyObject o, ICommand value)
        {
            o.SetValue(PerformRemovalProperty, value);
        }

        public static readonly DependencyProperty IsMarkedForRemovalProperty =
            DependencyProperty.RegisterAttached(
                "IsMarkedForRemoval",
                typeof(bool),
                typeof(AnimateItemRemovalBehavior),
                new UIPropertyMetadata(OnMarkedForRemovalChanged));

        public static bool GetIsMarkedForRemoval(DependencyObject o)
        {
            return o.GetValue(IsMarkedForRemovalProperty) as bool? ?? false;
        }

        public static void SetIsMarkedForRemoval(DependencyObject o, bool value)
        {
            o.SetValue(IsMarkedForRemovalProperty, value);
        }

        private static void OnMarkedForRemovalChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if ((e.NewValue as bool?) != true)
                return;

            var element = d as FrameworkElement;
            if (element == null)
                throw new InvalidOperationException(
                    "MarkedForRemoval can only be set on a FrameworkElement");

            var performRemoval = GetPerformRemoval(d);
            if (performRemoval == null)
                throw new InvalidOperationException(
                    "MarkedForRemoval requires PerformRemoval to be set too");

            var sb = GetStoryboard(d);
            if (sb == null)
                throw new InvalidOperationException(
                    "MarkedForRemoval requires Stoyboard to be set too");

            if (sb.IsSealed || sb.IsFrozen)
                sb = sb.Clone();

            Storyboard.SetTarget(sb, d);
            sb.Completed += (_, __) =>
            {
                var vm = element.DataContext;
                if (!performRemoval.CanExecute(vm))
                    return;
                performRemoval.Execute(vm);
            };

            sb.Begin();
        }
    }
}