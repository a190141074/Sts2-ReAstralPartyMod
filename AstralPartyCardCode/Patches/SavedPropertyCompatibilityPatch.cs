using System.Collections;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Patches;

[HarmonyPatch(typeof(SavedProperties), "FillInternal")]
public static class SavedPropertyCompatibilityPatch
{
    private static readonly BindingFlags InstancePropertyFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly BindingFlags InstanceFieldFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [HarmonyPrefix]
    public static bool Prefix(SavedProperties __instance, object model)
    {
        FillInternalCompat(__instance, model);
        return false;
    }

    private static void FillInternalCompat(SavedProperties savedProperties, object model)
    {
        var modelType = model.GetType();

        foreach (var listField in savedProperties.GetType().GetFields(InstanceFieldFlags))
        {
            if (!typeof(IEnumerable).IsAssignableFrom(listField.FieldType))
                continue;

            if (listField.GetValue(savedProperties) is not IEnumerable savedEntries)
                continue;

            foreach (var entry in savedEntries)
            {
                if (entry == null)
                    continue;

                var entryType = entry.GetType();
                var nameField = entryType.GetField("name", InstanceFieldFlags);
                var valueField = entryType.GetField("value", InstanceFieldFlags);
                if (nameField?.GetValue(entry) is not string propertyName || valueField == null)
                    continue;

                var property = modelType.GetProperty(propertyName, InstancePropertyFlags);
                if (property == null || !property.CanWrite)
                    continue;

                var rawValue = valueField.GetValue(entry);
                if (!TryConvertSavedValue(rawValue, property.PropertyType, out var convertedValue))
                {
                    MainFile.Logger.Warn(
                        $"Skipping incompatible saved property '{modelType.FullName}.{propertyName}' " +
                        $"with value type '{rawValue?.GetType().FullName ?? "null"}'."
                    );
                    continue;
                }

                try
                {
                    property.SetValue(model, convertedValue);
                }
                catch (Exception ex)
                {
                    MainFile.Logger.Warn(
                        $"Failed to apply saved property '{modelType.FullName}.{propertyName}' " +
                        $"from value type '{rawValue?.GetType().FullName ?? "null"}': {ex.Message}"
                    );
                }
            }
        }
    }

    private static bool TryConvertSavedValue(object? rawValue, Type propertyType, out object? convertedValue)
    {
        var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (rawValue == null)
        {
            convertedValue = null;
            return !targetType.IsValueType || Nullable.GetUnderlyingType(propertyType) != null;
        }

        if (targetType.IsInstanceOfType(rawValue))
        {
            convertedValue = rawValue;
            return true;
        }

        if (targetType.IsEnum && TryConvertToInt(rawValue, out var enumValue))
        {
            convertedValue = Enum.ToObject(targetType, enumValue);
            return true;
        }

        if (targetType == typeof(bool) && TryConvertToBool(rawValue, out var boolValue))
        {
            convertedValue = boolValue;
            return true;
        }

        if (targetType == typeof(int) && rawValue is bool boolAsInt)
        {
            convertedValue = boolAsInt ? 1 : 0;
            return true;
        }

        if (targetType.IsArray && TryConvertArray(rawValue, targetType, out convertedValue))
            return true;

        if (rawValue is IConvertible && typeof(IConvertible).IsAssignableFrom(targetType))
            try
            {
                convertedValue = Convert.ChangeType(rawValue, targetType, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
            }

        convertedValue = null;
        return false;
    }

    private static bool TryConvertArray(object rawValue, Type propertyType, out object? convertedValue)
    {
        convertedValue = null;
        if (rawValue is not Array rawArray || !propertyType.IsArray)
            return false;

        var elementType = propertyType.GetElementType();
        if (elementType == null)
            return false;

        if (propertyType.IsInstanceOfType(rawValue))
        {
            convertedValue = rawValue;
            return true;
        }

        if (!elementType.IsEnum)
            return false;

        var convertedArray = Array.CreateInstance(elementType, rawArray.Length);
        for (var i = 0; i < rawArray.Length; i++)
        {
            var value = rawArray.GetValue(i);
            if (!TryConvertToInt(value, out var enumValue))
                return false;

            convertedArray.SetValue(Enum.ToObject(elementType, enumValue), i);
        }

        convertedValue = convertedArray;
        return true;
    }

    private static bool TryConvertToBool(object rawValue, out bool value)
    {
        switch (rawValue)
        {
            case bool boolValue:
                value = boolValue;
                return true;
            case string stringValue when bool.TryParse(stringValue, out var parsedBool):
                value = parsedBool;
                return true;
            default:
                if (TryConvertToInt(rawValue, out var intValue))
                {
                    value = intValue != 0;
                    return true;
                }

                value = default;
                return false;
        }
    }

    private static bool TryConvertToInt(object? rawValue, out int value)
    {
        switch (rawValue)
        {
            case null:
                value = default;
                return false;
            case int intValue:
                value = intValue;
                return true;
            case bool boolValue:
                value = boolValue ? 1 : 0;
                return true;
            case IConvertible convertible:
                try
                {
                    value = Convert.ToInt32(convertible, CultureInfo.InvariantCulture);
                    return true;
                }
                catch
                {
                    break;
                }
        }

        value = default;
        return false;
    }
}