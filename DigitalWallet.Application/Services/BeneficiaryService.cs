using DigitalWallet.Application.DTOs.Beneficiary;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Exceptions;

namespace DigitalWallet.Application.Services;

public class BeneficiaryService : IBeneficiaryService
{
    private readonly IBeneficiaryRepository _beneficiaryRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditWriter _auditWriter;

    public BeneficiaryService(
        IBeneficiaryRepository beneficiaryRepository,
        IWalletRepository walletRepository,
        IUnitOfWork unitOfWork,
        IAuditWriter auditWriter
    )
    {
        _beneficiaryRepository = beneficiaryRepository;
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
    }

    public async Task<List<BeneficiaryResponseDto>> GetAllAsync(int userId)
    {
        List<Beneficiary> beneficiaries = await _beneficiaryRepository.GetAllByUserIdAsync(userId);
        return beneficiaries.Select(MapToDto).ToList();
    }

    public async Task<BeneficiaryResponseDto> AddAsync(int userId, AddBeneficiaryDto dto)
    {
        // 1. check wallet number exists in system
        Wallet? receiverWallet = await _walletRepository.GetByWalletNumberAsync(dto.WalletNumber);

        if (receiverWallet == null)
            throw new NotFoundException(nameof(Wallet), dto.WalletNumber);

        // 2. check user is not saving their own wallet
        Wallet? userWallet = await _walletRepository.GetByUserIdAsync(userId);

        if (userWallet != null && userWallet.WalletNumber == dto.WalletNumber)
            throw new WalletException("Cannot save your own wallet as a beneficiary");

        // 3. check not already saved
        bool alreadyExists = await _beneficiaryRepository.ExistsAsync(userId, dto.WalletNumber);

        if (alreadyExists)
            throw new WalletException("This wallet is already saved as a beneficiary");

        // 4. create and save
        Beneficiary beneficiary = new Beneficiary
        {
            Nickname = dto.Nickname,
            WalletNumber = dto.WalletNumber,
            UserId = userId,
        };

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            Beneficiary created = await _beneficiaryRepository.AddAsync(beneficiary);
            await _auditWriter.WriteBeneficiaryCreatedAsync(created);
            await _unitOfWork.CommitAsync();
            return MapToDto(created);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteAsync(int userId, int beneficiaryId)
    {
        Beneficiary? beneficiary = await _beneficiaryRepository.GetByIdAsync(beneficiaryId);
        if (beneficiary == null)
            throw new NotFoundException(nameof(Beneficiary), beneficiaryId);

        if (beneficiary.UserId != userId)
            throw new WalletException("You are not authorized to delete this beneficiary");
        await _beneficiaryRepository.DeleteAsync(beneficiary);
    }

    private static BeneficiaryResponseDto MapToDto(Beneficiary beneficiary)
    {
        return new BeneficiaryResponseDto
        {
            Id = beneficiary.Id,
            Nickname = beneficiary.Nickname,
            WalletNumber = beneficiary.WalletNumber,
            CreatedAt = beneficiary.CreatedAt,
        };
    }
}
