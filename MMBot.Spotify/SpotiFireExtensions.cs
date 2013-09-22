using System.Collections.Generic;
using System.Linq;
using SpotiFire;

namespace MMBot.Spotify
{
    public static class SpotiFireExtensions
    {
        public static string GetDisplayName(this Track track)
        {
            return string.Format("'{0}' by '{1}' from the album '{2}'", track.Name, track.Artists.GetDisplayName() , track.Album.Name);
        }
        
        private static string GetDisplayName(this IEnumerable<Artist> artists)
        {
            return string.Join(",", artists.Select(a => a.Name));
        }
    }
}