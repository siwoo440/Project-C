using UnityEngine;

[CreateAssetMenu(fileName = "BattleLoadout_New", menuName = "Project C/Data/Battle Loadout")]
public sealed class BattleLoadoutData : ScriptableObject
{
    [Header("전투 편성")]
    [SerializeField] private PartyData party; // 출전 파티 데이터
    [SerializeField] private DeckData deck; // 출전 공용 덱 데이터

    public PartyData Party => party; // 출전 파티 조회
    public DeckData Deck => deck; // 출전 덱 조회

    public bool IsValidLoadout()
    {
        if (party == null || deck == null) // 파티와 덱 존재 여부 확인
        {
            return false; // 잘못된 전투 편성 반환
        }

        if (!party.IsValidParty()) // 파티 유효성 확인
        {
            return false; // 잘못된 전투 편성 반환
        }

        if (!deck.IsValidDeck()) // 덱 유효성 확인
        {
            return false; // 잘못된 전투 편성 반환
        }

        if (!deck.AreAllOwnersInParty(party)) // 카드 소유자 출전 여부 확인
        {
            return false; // 잘못된 전투 편성 반환
        }

        return true; // 정상 전투 편성 반환
    }

    public PartyDeploymentValidationResult ValidateDeployment() // 현재 저장 상태 포함 출전 가능 여부 검증
    {
        if (!IsValidLoadout())
        {
            return new PartyDeploymentValidationResult(
                false,
                PartyDeploymentBlockReason.InvalidLoadout,
                null,
                0); // 구조적으로 잘못된 Loadout 차단
        }

        return PartyDeploymentValidator.Validate(
            party); // 사망·회복 상태 포함 파티 출전 검증
    }

    public bool IsDeployableLoadout() // 파티 편성 UI와 탐사 진입용 최종 출전 가능 여부 조회
    {
        return ValidateDeployment().CanDeploy; // 현재 런타임 상태 포함 출전 가능 반환
    }
}
