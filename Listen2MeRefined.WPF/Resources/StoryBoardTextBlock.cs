namespace Listen2MeRefined.WPF
{
    using System.Windows;
    using System.Windows.Controls;

    public class StoryboardTextBlock : TextBlock
    {
        public static readonly DependencyProperty StoryboardNameProperty =
        DependencyProperty.Register("StoryboardName", typeof(string), typeof(StoryboardTextBlock));

        public string StoryboardName
        {
            get { return (string)GetValue(StoryboardNameProperty) ?? ""; }
            set { SetValue(StoryboardNameProperty, value ?? ""); }
        }
    }
}
