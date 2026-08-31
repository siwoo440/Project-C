using System.Collections.Generic; // 실제 전투 출전 명단과 카드 목록 사용

public static class BattleCombatRosterBuilder // 전투 Scene 실제 출전 인원·카드 필터
{
    public static List<CharacterData> BuildDeployableMembers(
        IReadOnlyList<CharacterData> partyMembers) // 실제 런타임 출전 가능 캐릭터 생성
    {
        CharacterRecoveryManager recoveryManager = CharacterRecoveryManager.EnsureInstance(); // 회복 관리자 준비
        CharacterRecoveryDeploymentStatusProvider statusProvider =
            new CharacterRecoveryDeploymentStatusProvider(recoveryManager); // 출전 상태 제공자 준비

        return BuildDeployableMembers(
            partyMembers,
            statusProvider); // 공통 출전 명단 생성
    }

    public static List<CharacterData> BuildDeployableMembers(
        IReadOnlyList<CharacterData> partyMembers,
        IPartyDeploymentStatusProvider statusProvider) // 지정 상태 기반 실제 출전 명단 생성
    {
        List<CharacterData> deployableMembers = new List<CharacterData>(); // 출전 가능 캐릭터 목록 생성

        if (partyMembers == null)
        {
            return deployableMembers; // 빈 파티 입력 처리
        }

        for (int index = 0; index < partyMembers.Count; index++)
        {
            CharacterData member = partyMembers[index]; // 현재 파티원 조회

            if (member == null)
            {
                continue; // 빈 파티원 제외
            }

            if (statusProvider != null && !statusProvider.CanDeploy(member))
            {
                continue; // 사망·회복 중·출전 불가 캐릭터 제외
            }

            deployableMembers.Add(member); // 실제 전투 출전 캐릭터 등록
        }

        return deployableMembers; // 원래 파티 순서 유지 명단 반환
    }

    public static List<RunDeckCardEntry> FilterRunDeckCards(
        IReadOnlyList<RunDeckCardEntry> deckEntries,
        IReadOnlyList<CharacterData> deployableMembers) // 실제 출전 캐릭터 소유 카드 필터
    {
        List<RunDeckCardEntry> filteredCards = new List<RunDeckCardEntry>(); // 전투용 카드 목록 생성

        if (deckEntries == null || deployableMembers == null)
        {
            return filteredCards; // 빈 입력 처리
        }

        HashSet<CharacterData> deployableOwners =
            new HashSet<CharacterData>(deployableMembers); // 출전 캐릭터 검색 집합 생성

        for (int index = 0; index < deckEntries.Count; index++)
        {
            RunDeckCardEntry entry = deckEntries[index]; // 현재 회차 카드 조회

            if (entry == null || entry.Owner == null)
            {
                continue; // 잘못된 카드 항목 제외
            }

            if (!deployableOwners.Contains(entry.Owner))
            {
                continue; // 비출전 캐릭터 소유 카드 제외
            }

            filteredCards.Add(entry); // 실제 전투 카드 등록
        }

        return filteredCards; // 기존 카드 순서 유지 목록 반환
    }

    public static List<RunDeckCardEntry> FilterDeployableRunDeckCards(
        IReadOnlyList<RunDeckCardEntry> deckEntries) // 실제 상태 기반 전투 카드 필터
    {
        CharacterRecoveryManager recoveryManager = CharacterRecoveryManager.EnsureInstance(); // 회복 관리자 준비
        CharacterRecoveryDeploymentStatusProvider statusProvider =
            new CharacterRecoveryDeploymentStatusProvider(recoveryManager); // 출전 상태 제공자 준비

        return FilterDeployableRunDeckCards(
            deckEntries,
            statusProvider); // 공통 카드 필터 호출
    }

    public static List<RunDeckCardEntry> FilterDeployableRunDeckCards(
        IReadOnlyList<RunDeckCardEntry> deckEntries,
        IPartyDeploymentStatusProvider statusProvider) // 지정 상태 기반 전투 카드 필터
    {
        List<RunDeckCardEntry> filteredCards = new List<RunDeckCardEntry>(); // 실제 전투 카드 목록 생성

        if (deckEntries == null)
        {
            return filteredCards; // 빈 카드 목록 처리
        }

        for (int index = 0; index < deckEntries.Count; index++)
        {
            RunDeckCardEntry entry = deckEntries[index]; // 현재 회차 카드 조회

            if (entry == null || entry.Owner == null)
            {
                continue; // 잘못된 카드 항목 제외
            }

            if (statusProvider != null && !statusProvider.CanDeploy(entry.Owner))
            {
                continue; // 사망·회복 중 소유자 카드 제외
            }

            filteredCards.Add(entry); // 실제 전투 카드 등록
        }

        return filteredCards; // 기존 덱 순서 유지 목록 반환
    }
}
