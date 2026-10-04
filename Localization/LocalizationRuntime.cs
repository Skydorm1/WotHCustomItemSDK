using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WotHCustomItemSDK
{
    internal static class LocalizationRuntime
    {
        private static Type _languageSourceDataType;
        private static Type _termDataType;
        private static Type _termTypeEnum;
        private static object _source;

        public static bool AddOrUpdateTerm(
            string term,
            string languageCode,
            string translation,
            string description = "")
        {
            if (string.IsNullOrWhiteSpace(term) || string.IsNullOrWhiteSpace(languageCode))
                return false;

            try
            {
                object source = FindLanguageSource();
                if (source == null)
                    return false;

                IList languages = ReflectionUtil.GetIListMember(source, "mLanguages");
                if (languages == null)
                    languages = ReflectionUtil.GetIListMember(source, "Languages");
                if (languages == null)
                    return false;

                int languageIndex = FindLanguageIndex(source, languageCode.Trim());
                if (languageIndex < 0)
                {
                    string languageName = GetLanguageName(languageCode.Trim());
                    MethodInfo addLanguage = FindOptionalMethod(source.GetType(), "AddLanguage", 2);
                    if (addLanguage != null)
                    {
                        BuildAndInvoke(addLanguage, source, new object[] { languageName, languageCode.Trim() });
                        languageIndex = FindLanguageIndex(source, languageCode.Trim());
                    }
                }

                if (languageIndex < 0)
                    return false;

                object termData = InvokeGetTermData(source, term.Trim());
                if (termData == null)
                    termData = InvokeAddTerm(source, term.Trim(), GetTextTermType());
                if (termData == null)
                    return false;

                object textType = GetTextTermType();
                if (textType != null)
                    ReflectionUtil.SetMember(termData, "TermType", textType);
                ReflectionUtil.SetMember(termData, "Description", description ?? string.Empty);
                ReflectionUtil.Invoke(termData, "Validate");

                int languageCount = languages.Count;
                ResizeArrayMember(termData, "Languages", languageCount);
                ResizeArrayMember(termData, "Flags", languageCount);

                MethodInfo setTranslation = FindOptionalMethod(termData.GetType(), "SetTranslation", 2);
                if (setTranslation == null)
                    return false;

                object[] args = BuildOptionalArguments(
                    setTranslation,
                    new object[] { languageIndex, translation ?? string.Empty });
                setTranslation.Invoke(termData, args);
                ReflectionUtil.Invoke(source, "UpdateDictionary", true);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.LogError("[Localization] Failed to add/update term " + term + " [" + languageCode + "]: " + ex);
                return false;
            }
        }

        public static void EnsureTerm(string term, string translation)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                Plugin.LogWarning("[WotH Custom Item SDK Localization] Term darf nicht leer sein.");
                return;
            }

            try
            {
                object source = FindLanguageSource();
                if (source == null)
                {
                    Plugin.LogWarning(
                        "[WotH Custom Item SDK Localization] I2 LanguageSourceData could not be found."
                    );
                    return;
                }

                IList languages = ReflectionUtil.GetIListMember(source, "mLanguages");
                if (languages == null)
                    languages = ReflectionUtil.GetIListMember(source, "Languages");

                if (languages == null || languages.Count == 0)
                {
                    Plugin.LogWarning(
                        "[WotH Custom Item SDK Localization] I2 source has no languages."
                    );
                    return;
                }

                AddOrUpdateTerm(
                    term.Trim(),
                    languages.Count,
                    translation ?? string.Empty
                );

                ReflectionUtil.Invoke(source, "UpdateDictionary", true);

                Plugin.Log(
                    "[WotH Custom Item SDK Localization] Finished creating/updating term: " +
                    term + " = " + translation + " [all languages]"
                );
            }
            catch (Exception ex)
            {
                Plugin.LogError(
                    "[WotH Custom Item SDK Localization] Failed for term " + term + ": " + ex
                );
            }
        }

        public static void EnsureTerms(IEnumerable<string> names)
        {
            if (names == null)
                return;

            try
            {
                object source = FindLanguageSource();
                if (source == null)
                {
                    Plugin.LogWarning(
                        "[WotH Custom Item SDK Localization] I2 LanguageSourceData could not be found."
                    );
                    return;
                }

                IList languages = ReflectionUtil.GetIListMember(source, "mLanguages");
                if (languages == null)
                    languages = ReflectionUtil.GetIListMember(source, "Languages");

                if (languages == null || languages.Count == 0)
                {
                    Plugin.LogWarning(
                        "[WotH Custom Item SDK Localization] I2 source has no languages."
                    );
                    return;
                }

                foreach (string name in names)
                {
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    string cleanName = name.Trim();

                    AddOrUpdateTerm(
                        "UI/" + cleanName,
                        languages.Count,
                        cleanName
                    );
                }

                ReflectionUtil.Invoke(source, "UpdateDictionary", true);

                Plugin.Log(
                    "[WotH Custom Item SDK Localization] Finished creating/updating renamed-item terms."
                );
            }
            catch (Exception ex)
            {
                Plugin.LogError(
                    "[WotH Custom Item SDK Localization] Localization setup failed: " + ex
                );
            }
        }

        private static void AddOrUpdateTerm(
            string term,
            int languageCount,
            string translation)
        {
            object source = FindLanguageSource();
            if (source == null)
                return;

            object termData = InvokeGetTermData(source, term);

            bool wasCreated = false;

            if (termData == null)
            {
                object textTermType = GetTextTermType();
                termData = InvokeAddTerm(
                    source,
                    term,
                    textTermType
                );
                wasCreated = termData != null;
            }

            if (termData == null)
            {
                Plugin.LogWarning(
                    "[WotH Custom Item SDK Localization] Term konnte nicht erstellt werden: " + term
                );
                return;
            }

            object textType = GetTextTermType();
            if (textType != null)
                ReflectionUtil.SetMember(termData, "TermType", textType);

            ReflectionUtil.SetMember(
                termData,
                "Description",
                string.Empty
            );

            ReflectionUtil.Invoke(termData, "Validate");

            ResizeArrayMember(
                termData,
                "Languages",
                languageCount
            );

            ResizeArrayMember(
                termData,
                "Flags",
                languageCount
            );

            MethodInfo setTranslation = FindOptionalMethod(
                termData.GetType(),
                "SetTranslation",
                2
            );

            if (setTranslation == null)
            {
                Plugin.LogWarning(
                    "[WotH Custom Item SDK Localization] SetTranslation konnte nicht gefunden werden für " + term
                );
                return;
            }

            for (int languageIndex = 0;
                 languageIndex < languageCount;
                 languageIndex++)
            {
                object[] args = BuildOptionalArguments(
                    setTranslation,
                    new object[]
                    {
                        languageIndex,
                        translation ?? string.Empty
                    }
                );

                setTranslation.Invoke(termData, args);
            }

            Plugin.Log(
                "[WotH Custom Item SDK Localization] " +
                (wasCreated ? "Added" : "Updated") +
                " term: " + term +
                " = " + translation +
                " [all languages]"
            );
        }

        private static object InvokeGetTermData(object source, string term)
        {
            MethodInfo method = FindOptionalMethod(
                source.GetType(),
                "GetTermData",
                1
            );

            if (method == null)
            {
                Plugin.LogWarning(
                    "Method not found: " +
                    source.GetType().FullName +
                    ".GetTermData"
                );

                return null;
            }

            try
            {
                object[] args = BuildOptionalArguments(
                    method,
                    new object[] { term }
                );

                return method.Invoke(source, args);
            }
            catch (Exception ex)
            {
                Plugin.LogWarning(
                    "[WotH Custom Item SDK Localization] GetTermData failed for " +
                    term + ": " + ex.Message
                );

                return null;
            }
        }

        private static object InvokeAddTerm(
            object source,
            string term,
            object termType)
        {
            MethodInfo method = FindOptionalMethod(
                source.GetType(),
                "AddTerm",
                2
            );

            if (method == null)
                return null;

            try
            {
                object[] args = BuildOptionalArguments(
                    method,
                    new object[] { term, termType }
                );

                return method.Invoke(source, args);
            }
            catch (Exception ex)
            {
                Plugin.LogWarning(
                    "[WotH Custom Item SDK Localization] AddTerm failed for " +
                    term + ": " + ex.Message
                );

                return null;
            }
        }

        private static MethodInfo FindOptionalMethod(
            Type type,
            string name,
            int suppliedParameterCount)
        {
            if (type == null)
                return null;

            MethodInfo[] methods = type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != name)
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length < suppliedParameterCount)
                    continue;

                bool valid = true;
                for (int p = suppliedParameterCount; p < parameters.Length; p++)
                {
                    if (!parameters[p].IsOptional)
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid)
                    return method;
            }

            return null;
        }

        private static object[] BuildOptionalArguments(
            MethodInfo method,
            object[] supplied)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object[] result = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i < supplied.Length)
                {
                    result[i] = supplied[i];
                }
                else
                {
                    object defaultValue = parameters[i].DefaultValue;
                    if (defaultValue == Missing.Value)
                    {
                        defaultValue = GetDefault(parameters[i].ParameterType);
                    }

                    result[i] = defaultValue;
                }
            }

            return result;
        }

        private static object GetDefault(Type type)
        {
            if (type == null || !type.IsValueType)
                return null;

            return Activator.CreateInstance(type);
        }

        private static object GetTextTermType()
        {
            EnsureTypes();

            if (_termTypeEnum == null || !_termTypeEnum.IsEnum)
                return null;

            try
            {
                return Enum.Parse(
                    _termTypeEnum,
                    "Text"
                );
            }
            catch
            {
                return Activator.CreateInstance(_termTypeEnum);
            }
        }

        private static int FindLanguageIndex(object source, string languageCode)
        {
            MethodInfo method = FindOptionalMethod(source.GetType(), "GetLanguageIndexFromCode", 1);
            if (method == null)
                return -1;

            try
            {
                object[] args = BuildOptionalArguments(method, new object[] { languageCode });
                object result = method.Invoke(source, args);
                return result == null ? -1 : Convert.ToInt32(result);
            }
            catch
            {
                return -1;
            }
        }

        private static void BuildAndInvoke(MethodInfo method, object target, object[] supplied)
        {
            method.Invoke(target, BuildOptionalArguments(method, supplied));
        }

        private static string GetLanguageName(string languageCode)
        {
            switch (languageCode.ToLowerInvariant())
            {
                case "en": return "English";
                case "de": return "German";
                case "fr": return "French";
                case "es": return "Spanish";
                case "it": return "Italian";
                case "pt": return "Portuguese";
                case "ru": return "Russian";
                case "ja": return "Japanese";
                case "ko": return "Korean";
                case "zh": return "Chinese";
                case "pl": return "Polish";
                case "nl": return "Dutch";
                default: return languageCode;
            }
        }

        private static object FindLanguageSource()
        {
            if (_source != null)
                return _source;

            EnsureTypes();

            if (_languageSourceDataType == null)
                return null;

            Type localizationManager =
                ReflectionUtil.TypeByName(
                    "I2.Loc.LocalizationManager"
                );

            if (localizationManager == null)
                return null;

            object sources = ReflectionUtil.GetStaticMember(
                localizationManager,
                "Sources"
            );

            IList sourceList = sources as IList;
            if (sourceList == null)
                return null;

            for (int i = 0; i < sourceList.Count; i++)
            {
                object candidate = sourceList[i];
                if (candidate != null &&
                    _languageSourceDataType.IsInstanceOfType(candidate))
                {
                    _source = candidate;
                    return _source;
                }
            }

            return null;
        }

        private static void EnsureTypes()
        {
            if (_languageSourceDataType == null)
            {
                _languageSourceDataType =
                    ReflectionUtil.TypeByName(
                        "I2.Loc.LanguageSourceData"
                    );
            }

            if (_termDataType == null)
            {
                _termDataType =
                    ReflectionUtil.TypeByName(
                        "I2.Loc.TermData"
                    );
            }

            if (_termTypeEnum == null &&
                _termDataType != null)
            {
                FieldInfo field = _termDataType.GetField(
                    "TermType",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

                if (field != null)
                    _termTypeEnum = field.FieldType;
            }
        }

        private static MethodInfo FindMethod(
            Type type,
            string name,
            int parameterCount)
        {
            if (type == null)
                return null;

            MethodInfo[] methods = type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name == name &&
                    method.GetParameters().Length == parameterCount)
                {
                    return method;
                }
            }

            return null;
        }

        private static void ResizeArrayMember(
            object instance,
            string memberName,
            int length)
        {
            object value = ReflectionUtil.GetMember(
                instance,
                memberName
            );

            Array array = value as Array;

            if (array == null || array.Length < length)
            {
                Type type = instance.GetType();
                FieldInfo field = AccessTools.Field(
                    type,
                    memberName
                );

                if (field == null || !field.FieldType.IsArray)
                    return;

                Array resized = Array.CreateInstance(
                    field.FieldType.GetElementType(),
                    length
                );

                if (array != null)
                    Array.Copy(array, resized, array.Length);

                ReflectionUtil.SetMember(
                    instance,
                    memberName,
                    resized
                );
            }
        }
    }
}
