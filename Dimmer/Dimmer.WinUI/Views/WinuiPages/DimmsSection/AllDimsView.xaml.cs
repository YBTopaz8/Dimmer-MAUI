using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.DimmsSection
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AllDimsView : Page
    {
        public AllDimsView()
        {
            InitializeComponent();
        }

        private void MoreBtn_Click(object sender, RoutedEventArgs e)
        {
            var MoreBtn = (Button)sender;
            var moreBtnFlyout = new MenuFlyout()
                ;


            var addNoteToSongMFItem = new MenuFlyoutItem { Text = "Add Note to Song" };
            FontIcon iconNote = new FontIcon();
            iconNote.Glyph = "\uF7BB";
            addNoteToSongMFItem.Icon = iconNote;

            moreBtnFlyout.Items.Add(addNoteToSongMFItem);

            FontIcon LoveIcon = new FontIcon();
            LoveIcon.Glyph = "\uEB51";
            var menuItemOne = new MenuFlyoutItem()
            {
                Text = "Love",
                Icon = LoveIcon
            };
            moreBtnFlyout.Items.Add(menuItemOne);


            FontIcon EditIcon = new FontIcon();
            EditIcon.Glyph = "\uE70F";
            var menuItemTwo = new MenuFlyoutItem()
            {
                Text = "Edit",
                Icon = EditIcon
            };

            moreBtnFlyout.Items.Add(menuItemTwo);


            FontIcon Deleteicon = new FontIcon();
            Deleteicon.Glyph = "\uE74D";
            var menuItemThree = new MenuFlyoutItem()
            {
                Text = "Delete",
                Icon = Deleteicon
                ,
              
            };

            moreBtnFlyout.Items.Add(menuItemThree);


            MenuFlyoutSubItem moreSubSection = new MenuFlyoutSubItem();

                
            var searchFlyoutItem = new MenuFlyoutItem()
            { Text = "Search" };
            var ShareFlyoutItem = new MenuFlyoutItem()
            { Text = "Share" }; 
            moreSubSection.Items.Add(searchFlyoutItem);
            moreSubSection.Items.Add(ShareFlyoutItem);

            moreBtnFlyout.Items.Add(moreSubSection);
            FlyoutShowOptions flyoutShowOpt = new FlyoutShowOptions
            {
                Placement = FlyoutPlacementMode.Auto,
                ShowMode = FlyoutShowMode.Auto
            };
            moreBtnFlyout.ShowAt(MoreBtn, flyoutShowOpt);
        }
    }
}
