namespace Drexel.VidUp.Business
{
    public class Category
    {
        public int Id { get; }
        public string Name { get; }

        public static Category[] Categories { get; }

        public Category(int id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        static Category()
        {
            Category.Categories = new Category[]
            {
                new Category(1, "Film & Animation"),
                new Category(2, "Autos & Vehicles"),
                new Category(10, "Music"),
                new Category(15, "Pets & Animals"),
                new Category(17, "Sports"),
                new Category(18, "Short Movies"),
                new Category(19, "Travel & Events"),
                new Category(20, "Gaming"),
                new Category(21, "Videoblogging"),
                new Category(22, "People & Blogs"),
                new Category(23, "Comedy"),
                new Category(24, "Entertainment"),
                new Category(25, "News & Politics"),
                new Category(26, "Howto & Style"),
                new Category(27, "Education"),
                new Category(28, "Science & Technology"),
                new Category(29, "Nonprofits & Activism"),
                new Category(30, "Movies"),
                new Category(31, "Anime/Animation"),
                new Category(32, "Action/Adventure"),
                new Category(33, "Classics"),
                new Category(34, "Comedy"),
                new Category(35, "Documentary"),
                new Category(36, "Drama"),
                new Category(37, "Family"),
                new Category(38, "Foreign"),
                new Category(39, "Horror"),
                new Category(40, "Sci-Fi/Fantasy"),
                new Category(41, "Thriller"),
                new Category(42, "Shorts"),
                new Category(43, "Shows"),
                new Category(44, "Trailers")
            };
        }
    }
}