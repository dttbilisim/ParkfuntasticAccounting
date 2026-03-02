using ecommerce.Core.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace ecommerce.Admin.Services;

public class AuthenticationCircuitHandler : CircuitHandler, IDisposable
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly CurrentUser _currentUser;
    private readonly AuthenticationService _authenticationService;

    public AuthenticationCircuitHandler(
        AuthenticationStateProvider authenticationStateProvider,
        CurrentUser currentUser,
        AuthenticationService authenticationService)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _currentUser = currentUser;
        _authenticationService = authenticationService;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _authenticationStateProvider.AuthenticationStateChanged += AuthenticationChanged;

        return base.OnCircuitOpenedAsync(circuit, cancellationToken);
    }

    private void AuthenticationChanged(Task<AuthenticationState> task)
    {
        _ = UpdateAuthentication(task);

        async Task UpdateAuthentication(Task<AuthenticationState> innerTask)
        {
            try
            {
                var state = await innerTask;
                _currentUser.SetUser(state.User);

                await _authenticationService.InitializeAsync(state);
            }
            catch
            {
                // ignored
            }
        }
    }

    public override async Task OnConnectionUpAsync(
        Circuit circuit,
        CancellationToken cancellationToken)
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        _currentUser.SetUser(state.User);

        await _authenticationService.InitializeAsync(state);
    }

    public void Dispose()
    {
        _authenticationStateProvider.AuthenticationStateChanged -= AuthenticationChanged;
    }
}