using PocketBudget.ViewModels;

namespace PocketBudget.Views;

public partial class DetailPage : ContentPage
{
    public DetailPage()
    {
        InitializeComponent();

        BindingContext = new DetailViewModel();
    }
}