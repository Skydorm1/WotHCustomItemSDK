using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WotHCustomItemSDK
{
    [Serializable]
    public class WotHLocalizationData
    {
        public int id;

        public List<TranslationData> itemNameTranslations =
            new List<TranslationData>();

        public List<TranslationData> itemInfoTranslations =
            new List<TranslationData>();

        [Serializable]
        public class TranslationData
        {
            public string languageCode;
            public string translation;
        }
    }
}
