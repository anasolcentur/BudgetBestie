using PocketBudget.Services;
using PocketBudget.ViewModels;

namespace PocketBudget.Views;

public partial class MainPage : ContentPage
{
    private bool _dailyAdviceShown;

    public MainPage()
    {
        InitializeComponent();

        BindingContext = new MainViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_dailyAdviceShown)
        {
            return;
        }

        _dailyAdviceShown = true;

        await ShowDailyAdviceAsync();
    }

    private async Task ShowDailyAdviceAsync()
    {
        try
        {
            var adviceService = new AdviceService();

            var advice = await adviceService.GetRandomAdviceAsync();

            await DisplayAlert(
                "Consejo del día 💡",
                advice,
                "Empezar");
        }
        catch
        {
            await DisplayAlert(
                "Consejo del día 💡",
                "Antes de comprar algo, preguntate si realmente lo necesitás o si solo querés una mini dosis de dopamina con ticket.",
                "Empezar");
        }
    }
}