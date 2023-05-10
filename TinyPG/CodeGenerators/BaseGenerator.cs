namespace TinyPG.CodeGenerators
{
    public class BaseGenerator
    {
        protected string TemplateName;

        public BaseGenerator(string templateName)
        {
            TemplateName = templateName;
            FileName = templateName;

        }

        public virtual string FileName { get; set; }
    }
}
