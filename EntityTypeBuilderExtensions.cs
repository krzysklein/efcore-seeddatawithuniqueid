using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

public static class EntityTypeBuilderExtensions
{
    public static DataBuilder<TEntity> SeedDataWithUniqueId<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, [NotNull] params object[] data)
        where TEntity : class
    {
        return entityTypeBuilder.HasData(data.Select(obj => _CopyObjectWithGuidId<TEntity>(obj)));
    }

    private static T _CopyObjectWithGuidId<T>(object obj)
    {
        var copy = _CopyObject<T>(obj);
        var guid = _GetObjectGuidHash(copy);
        var final = _CopyObject(copy, new { Id = guid });
        return final;
    }

    private static T _CopyObject<T>(object source)
    {
        var instance = (T)Activator.CreateInstance(typeof(T), nonPublic: true);
        return _CopyObject(instance, source);
    }

    private static T _CopyObject<T>(T instance, object source)
    {
        var sourceType = source.GetType();
        var destType = typeof(T);

        foreach (var sourceProp in sourceType.GetProperties())
        {
            var destProp = destType.GetProperty(sourceProp.Name);
            if (destProp == null) continue;

            var value = sourceProp.GetValue(source);

            if (destProp.SetMethod != null)
            {
                destProp.SetValue(instance, value);
            }
            else
            {
                var backingField = destType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(f => f.Name.StartsWith($"<{sourceProp.Name}>"));
                if (backingField != null)
                {
                    backingField.SetValue(instance, value);
                }
            }
        }

        return instance;
    }

    private static Guid _GetObjectGuidHash<T>(T instance)
    {
        var type = typeof(T);
        var typeHash = _GetDeterministicStringHashCode(type.FullName);
        var finalFieldTypeHash = 0;
        var finalFieldValueHash = 0;

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var fieldType = field.FieldType;
            var fieldTypeHash = _GetDeterministicStringHashCode(fieldType.FullName);
            finalFieldTypeHash ^= fieldTypeHash;

            var fieldValue = field.GetValue(instance);
            var fieldValueHash = fieldType == typeof(string)
                ? _GetDeterministicStringHashCode(fieldValue as string)
                : (fieldValue?.GetHashCode() ?? 0);
            finalFieldValueHash ^= fieldValueHash;
        }
        var instanceHash = ~finalFieldTypeHash ^ ~finalFieldValueHash;

        var bytes = new byte[16];
        Array.Copy(BitConverter.GetBytes(typeHash), 0, bytes, 0, 4);
        Array.Copy(BitConverter.GetBytes(instanceHash), 0, bytes, 4, 4);
        Array.Copy(BitConverter.GetBytes(finalFieldTypeHash), 0, bytes, 8, 4);
        Array.Copy(BitConverter.GetBytes(finalFieldValueHash), 0, bytes, 12, 4);

        var result = new Guid(bytes);
        return result;
    }

    /// <summary>
    /// Based on https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/
    /// </summary>
    private static int _GetDeterministicStringHashCode(string str)
    {
        unchecked
        {
            int hash1 = (5381 << 16) + 5381;
            int hash2 = hash1;

            for (int i = 0; i < str.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                if (i == str.Length - 1)
                    break;
                hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
            }

            return hash1 + (hash2 * 1566083941);
        }
    }
}
