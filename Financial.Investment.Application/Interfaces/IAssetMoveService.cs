using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

/// <summary>
/// Relocates an asset from one portfolio to another.
/// </summary>
/// <remarks>
/// Unlike its neighbours, which return a nullable DTO and let the API collapse null into a 400,
/// this method throws. A move can be refused for several distinct reasons and the user has to be
/// told which one, in the same words whether they are in the web app or the desktop app. An
/// exception carries that sentence from the domain to both front ends unchanged; a null return
/// carries nothing.
/// </remarks>
public interface IAssetMoveService
{
    /// <summary>
    /// Moves an asset into another portfolio of the same broker, existing or created by the move,
    /// and returns it read back from its new location.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The broker, source portfolio, or asset does not exist.</exception>
    /// <exception cref="ArgumentException">A required field is missing, or the scope is unrecognised.</exception>
    /// <exception cref="Domain.Exceptions.InvestmentRuleViolationException">A move rule refused it.</exception>
    Task<AssetDetailsDTO> MoveAssetAsync(MoveAssetRequestDTO request);

    /// <summary>
    /// Retires a fully closed asset from Active Investments into a Historic portfolio of the same
    /// broker, and returns it read back from there.
    /// </summary>
    /// <remarks>
    /// Its own method rather than a scope pair on <see cref="MoveAssetAsync"/>: the direction is
    /// fixed, the precondition is different - the position must be closed - and the broker's
    /// Historic record may have to be brought into being. Keeping it separate is also what makes
    /// the Historic-to-Active direction unexpressible rather than merely refused.
    /// </remarks>
    /// <exception cref="KeyNotFoundException">The Active broker, portfolio, or asset does not exist.</exception>
    /// <exception cref="ArgumentException">A required field is missing.</exception>
    /// <exception cref="Domain.Exceptions.InvestmentRuleViolationException">
    /// The asset still holds a position, or the Historic destination refuses it.
    /// </exception>
    Task<AssetDetailsDTO> ArchiveAssetAsync(ArchiveAssetRequestDTO request);
}
