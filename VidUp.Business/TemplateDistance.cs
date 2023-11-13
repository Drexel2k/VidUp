namespace Drexel.VidUp.Business
{
    public class TemplateDistance
    {
        private int distance;
        private Template template;
        
        public int Distance 
        {
            get => this.distance;
        }

        public Template Template
        {
            get => this.template;
        }

        public TemplateDistance(int distance, Template template)
        {
            this.distance = distance;
            this.template = template;
        }
    }
}
