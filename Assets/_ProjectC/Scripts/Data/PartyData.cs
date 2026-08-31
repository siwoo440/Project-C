using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Party_New", menuName = "Project C/Data/Party")]
public sealed class PartyData : ScriptableObject
{
    public const int MaxPartySize = 4;

    [Header("기본 정보")]
    [SerializeField] private string partyId; // 파티 고유 ID
    [SerializeField] private string displayName; // 파티 표시 이름

    [Header("출전 캐릭터")]
    [SerializeField] private List<CharacterData> members = new List<CharacterData>(); // 파티 캐릭터 목록

    public string PartyId => partyId; // 파티 ID 조회
    public string DisplayName => displayName; // 파티 이름 조회

    public IReadOnlyList<CharacterData> Members
    {
        get
        {
            if (!BattleCombatRosterRuntime.ShouldFilterCurrentScene)
            {
                return members; // 탐사·거점에서는 원래 편성 전체 조회
            }

            return BattleCombatRosterBuilder.BuildDeployableMembers(members); // 전투 Scene에서는 실제 출전 가능 캐릭터만 조회
        }
    }

    public int MemberCount => Members.Count; // 현재 Scene 기준 파티 인원 조회

    public bool ContainsCharacter(CharacterData character)
    {
        return character != null && members.Contains(character); // 원본 편성 포함 여부 반환
    }

    public bool IsValidParty()
    {
        if (members.Count < 1 || members.Count > MaxPartySize) // 원본 파티 인원 범위 확인
        {
            return false; // 잘못된 파티 반환
        }

        HashSet<CharacterData> uniqueMembers = new HashSet<CharacterData>(); // 중복 확인용 목록 생성

        foreach (CharacterData member in members) // 원본 파티원 순회
        {
            if (member == null) // 빈 캐릭터 확인
            {
                return false; // 잘못된 파티 반환
            }

            if (!uniqueMembers.Add(member)) // 중복 캐릭터 확인
            {
                return false; // 잘못된 파티 반환
            }
        }

        return true; // 정상 파티 반환
    }

    private void OnValidate()
    {
        while (members.Count > MaxPartySize) // 최대 파티 인원 초과 확인
        {
            members.RemoveAt(members.Count - 1); // 초과 파티원 제거
        }
    }
}
