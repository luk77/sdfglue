//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Settings;

namespace SingleDocAppFramework.Ui.Settings
{
    // Data passed to SettingsSection.Build
    public class SettingsBuildContext
    {
        public  IUiExecutorFramework    Executor;
        public  UserSettingsBase        Settings;           // edited in place (changes are detected by the settings dialog)
        public  int                     Index       = 1;    // ImGui id counter for the Ui* property editors (ref ctx.Index)

        public SettingsBuildContext(IUiExecutorFramework executor, UserSettingsBase settings)
        {
            Executor    = executor;
            Settings    = settings;
        }

        // Application-specific settings class
        public T GetSettings<T>() where T : UserSettingsBase
        {
            return (T)Settings;
        }
    }

    // Part of a settings category: properties drawn in a property grid and their reset to default values.
    // Reset copies only the fields edited by Build (settings <- defaults), so "Reset to default"
    // of a category does not change settings of other categories.
    public class SettingsSection
    {
        public delegate void BuildContent   (SettingsBuildContext ctx);
        public delegate void ResetToDefault (UserSettingsBase settings, UserSettingsBase defaults);

        public  string?                 Title;      // optional header, null - properties only
        public  BuildContent            Build;
        public  ResetToDefault?         Reset;      // null - the section has nothing to reset

        public SettingsSection(string? title, BuildContent build, ResetToDefault? reset)
        {
            Title   = title;
            Build   = build;
            Reset   = reset;
        }
    }

    // Page of the settings dialog (an item of the list on the left side)
    public class SettingsCategory
    {
        public  string                  Id;         // stable key used by SettingsRegistry.ExtendCategory
        public  string                  Title;
        public  int                     Order;      // position in the list (ascending)

        private List<SettingsSection>   sections_   = new List<SettingsSection>();

        public SettingsCategory(string id, string title, int order)
        {
            Id      = id;
            Title   = title;
            Order   = order;
        }

        public IReadOnlyList<SettingsSection> Sections { get { return sections_; } }

        public SettingsCategory AddSection(SettingsSection section)
        {
            sections_.Add(section);
            return this;
        }

        public SettingsCategory AddSection(string? title, SettingsSection.BuildContent build, SettingsSection.ResetToDefault? reset)
        {
            return AddSection(new SettingsSection(title, build, reset));
        }

        public void ResetToDefault(UserSettingsBase settings, UserSettingsBase defaults)
        {
            foreach(SettingsSection section in sections_)
                section.Reset?.Invoke(settings, defaults);
        }
    }

    // Categories shown in the settings dialog. The framework registers its categories
    // (FrameworkSettingsCategories), the application adds its own ones and extends the framework ones
    // (UiManagerBase.RegisterSettingsCategories).
    public class SettingsRegistry
    {
        private List<SettingsCategory>  categories_ = new List<SettingsCategory>();

        public bool IsEmpty { get { return categories_.Count == 0; } }

        public SettingsCategory AddCategory(SettingsCategory category)
        {
            if (FindCategory(category.Id) != null)
                throw new ArgumentException(String.Format("Settings category already registered: {0}", category.Id));

            categories_.Add(category);
            return category;
        }

        public SettingsCategory AddCategory(string id, string title, int order)
        {
            return AddCategory(new SettingsCategory(id, title, order));
        }

        // Adds a section at the end of an already registered category
        public SettingsCategory ExtendCategory(string id, SettingsSection section)
        {
            SettingsCategory? category = FindCategory(id);
            if (category == null)
                throw new ArgumentException(String.Format("Unknown settings category: {0}", id));

            return category.AddSection(section);
        }

        public SettingsCategory? FindCategory(string id)
        {
            return categories_.Find(c => c.Id == id);
        }

        // Sorted by Order (registration order for equal values)
        public IEnumerable<SettingsCategory> GetCategories()
        {
            return categories_.OrderBy(c => c.Order);
        }
    }
}
