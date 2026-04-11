using System.Windows.Markup;

namespace Listen2MeRefined.WPF.Utils;

public sealed class EnumValuesExtension(Type enumType) : MarkupExtension
{
    public Type EnumType { get; } = enumType;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        Enum.GetValues(EnumType);
}
