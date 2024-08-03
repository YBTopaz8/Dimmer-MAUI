using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Models;

public class Content
{
    public int id { get; set; }
    public string name { get; set; }
    public string trackName { get; set; }
    public string artistName { get; set; }
    public string albumName { get; set; }
    public float duration { get; set; }
    public bool instrumental { get; set; }
    public string plainLyrics { get; set; }
    public string syncedLyrics { get; set; }
}

public class LyristApiResponse
{
    public string lyrics { get; set; }
    public string title { get; set; }
    public string artist { get; set; }
    public string image { get; set; }

}