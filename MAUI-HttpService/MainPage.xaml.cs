using MAUI_HttpService.Models;
using MAUI_HttpService.Services;
using Newtonsoft.Json;

namespace MAUI_HttpService;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnCounterClicked(object sender, EventArgs e)
	{
		CounterBtn.IsEnabled = false;
		loadingIndicatior.IsVisible = true;
		resultLayout.IsVisible = false;

		//var result = await HttpManager.GetAsync("Status");

        var json = JsonConvert.SerializeObject(new LoginDTO(
			"USERNAME",
            "PASSWORD",
            "API-TOKEN",
            "APP-VERSION"));
		var result = await HttpManager.PostAsync("Auth/Login", json);
		
		resultLbl.Text = result;

        CounterBtn.IsEnabled = true;
        loadingIndicatior.IsVisible = false;
        resultLayout.IsVisible = true;
    }
}

