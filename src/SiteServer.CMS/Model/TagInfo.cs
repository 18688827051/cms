using Datory;

namespace SiteServer.CMS.Model
{
    [Table("siteserver_Tag")]
    public class TagInfo : Entity
    {
        [TableColumn]
        public int SiteId { get; set; }

        [TableColumn(Text = true)]
        public string ContentIdCollection { get; set; }

        [TableColumn]
        public string Tag { get; set; }

        [TableColumn]
        public int UseNum { get; set; }

        [TableColumn]
        public int Level { get; set; }
    }
}
