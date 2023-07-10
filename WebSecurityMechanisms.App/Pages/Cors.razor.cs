using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using WebSecurityMechanisms.Models;

namespace WebSecurityMechanisms.App.Pages;

public partial class Cors : ComponentBase
{
    private IJSObjectReference? _jsModule;
    private CorsBrowserNavigationData? _testResult;
    ElementReference _renderTo; 

    private List<Endpoint>? _endpoints;
    private List<Preset>? _presets;

    private Endpoint? _selectedEndpoint;
    private Preset? _selectedPreset;

    private bool IsLoaderVisible { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await Js.InvokeAsync<IJSObjectReference>("import", "./Pages/Cors.razor.js");
            _presets = await Http.GetFromJsonAsync<List<Preset>>(string.Concat(Configuration["ApiUrl"], "/Cors/ListPresets"));
            _endpoints = await Http.GetFromJsonAsync<List<Endpoint>>(string.Concat(Configuration["ApiUrl"], "/Cors/ListEndpoints"));
            if (_presets != null) _selectedPreset = _presets.First();
            if (_endpoints != null) _selectedEndpoint = _endpoints.First();
            
                if (_selectedEndpoint != null && _selectedPreset != null)
                    await LoadPresetAsync(_selectedEndpoint.Path, _selectedPreset.Key);
            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task TestConfigurationAsync()
    {
        IsLoaderVisible = true;

        var payload = await _jsModule!.InvokeAsync<string>("editor.getValue");
        string url = string.Concat(Configuration["ApiUrl"], "/Cors/TestConfiguration");
        var response = await Http.PostAsJsonAsync(url, payload);
        _testResult = await response.Content.ReadFromJsonAsync<CorsBrowserNavigationData>();

        IsLoaderVisible = false;
        
        if (_testResult != null)
        {
            await Js.InvokeVoidAsync("renderDiagram", _testResult.SequenceDiagram, _renderTo);
            StateHasChanged();
        }
        else if (_testResult == null || _testResult.IsInError)
        {
            SnackbarService.Add("An error occurred, please try again or report an issue !", Severity.Error);
        }
    }

    private async Task OnEndpointChangedAsync(IEnumerable<Endpoint> selectedItem)
    {
        if (_selectedEndpoint != null && _selectedPreset != null)
            await LoadPresetAsync(_selectedEndpoint.Path, _selectedPreset.Key);
    }
    
    private async Task OnPresetChangedAsync(IEnumerable<Preset> selectedItem)
    {
        if (_selectedEndpoint != null && _selectedPreset != null)
            await LoadPresetAsync(_selectedEndpoint.Path, _selectedPreset.Key);
    }

    private async Task LoadPresetAsync(string endpoint, string preset)
    {
        var result = await Http.GetStringAsync(string.Concat(Configuration["ApiUrl"], "/Cors/GetPreset?preset=", preset, "&endpoint=", endpoint));
        await _jsModule!.InvokeAsync<string>("editor.setValue", result);
    }

    readonly Func<Endpoint,string?> _endpointConverter = e => e.Path;
    readonly Func<Preset,string?> _presetConverter = e => e.Name;
}