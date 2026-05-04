namespace FormDezigner.Models
{
    public class FormComponent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "Text"; // Text, Dropdown, Checkbox, Radio, DatePicker, FileUpload
        public string Label { get; set; } = "New Component";
        public string Name { get; set; } = "field_name";
        public bool IsRequired { get; set; } = false;
        public string Placeholder { get; set; } = "";
        public string Options { get; set; } = ""; // Comma separated for dropdown/radio
    }

    public class FormDesign
    {
        public List<FormComponent> Components { get; set; } = new List<FormComponent>();
    }

    public class FormExportModel
    {
        public string FormName { get; set; } = string.Empty;
        public FormDesign Design { get; set; } = new FormDesign();
        public string BackendCode { get; set; } = string.Empty;
        public string BackendLanguage { get; set; } = "JS";
    }
}
