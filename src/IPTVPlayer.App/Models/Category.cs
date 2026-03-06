using System.Collections.ObjectModel;

namespace IPTVPlayer.App.Models;

public class Category
{
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<Channel> Channels { get; set; } = [];
}
