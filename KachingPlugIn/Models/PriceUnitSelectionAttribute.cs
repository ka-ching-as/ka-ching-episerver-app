using System;
using EPiServer.Cms.Shell.UI.ObjectEditing.EditorDescriptors.SelectionFactories;
using EPiServer.Shell.ObjectEditing;

namespace KachingPlugIn.KachingPlugIn.Models
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PriceUnitSelectionAttribute : SelectOneAttribute
    {
        public override Type SelectionFactoryType
        {
            get => typeof(LanguageSelectionFactory);
            set => throw new NotSupportedException();
        }
    }
}
