using System.Collections; // 플레이 모드 대기 사용
using NUnit.Framework; // 긴급 대기 검증 사용
using UnityEngine; // 게임 객체와 캐릭터 데이터 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 거점 버튼 사용

public sealed class EmergencyRecoveryWaitTests // 전원 출전 불가 진행 검사
{ // 긴급 대기 검사 시작
    [UnityTest] // 플레이 모드 검사 지정
    public IEnumerator LobbyWait_OnlyWhenAllOwnedUnavailableAndNoRewards() // 전체 출전 불가 시 무료 대기 확인
    { // 긴급 대기 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 실제 캐릭터 목록 조회
        CharacterRecoveryManager recovery = CharacterRecoveryManager.EnsureInstance(); // 실제 회복 관리자 준비
        BattleResultManager results = BattleResultManager.EnsureInstance(); // 실제 캐릭터 상태 관리자 준비
        PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 실제 자원 관리자 준비
        CharacterProgressionManager progression = CharacterProgressionManager.EnsureInstance(); // 실제 성장 관리자 준비
        CharacterAffinityManager affinity = CharacterAffinityManager.EnsureInstance(); // 실제 호감도 관리자 준비
        ExplorationSessionManager session = ExplorationSessionManager.EnsureInstance(); // 실제 탐사 상태 관리자 준비
        recovery.ResetRecoveryState(); // 기존 회복 정보 초기화
        results.ResetSavedPartyState(); // 기존 캐릭터 상태 초기화
        resources.ResetResources(); // 기존 재화 초기화
        resources.AddResources(100, 20, 30, 40); // 기존 보유 재화 설정
        int previousLevel = progression.Level; // 기존 성장 레벨 보관
        int previousExperience = progression.CurrentExperience; // 기존 경험치 보관
        int previousAffinity = affinity.Affinity; // 기존 호감도 보관
        int previousCompletion = session.CompletionVersion; // 기존 탐사 완료 순번 보관
        GameObject canvasObject = new GameObject("EmergencyCanvas", typeof(RectTransform), typeof(Canvas)); // 독립 거점 캔버스 생성
        LobbyLayoutView lobby = LobbyLayoutView.Create(canvasObject.GetComponent<Canvas>(), null, null, null); // 실제 거점 화면 생성
        Transform wait = lobby.transform.Find("EmergencyWaitButton"); // 긴급 대기 버튼 조회
        Assert.IsNotNull(wait); // 긴급 대기 버튼 존재 확인
        Assert.IsFalse(wait.gameObject.activeSelf); // 출전 가능 캐릭터가 있으면 대기 숨김 확인
        int owned = 0; // 보유 캐릭터 수 초기화
        foreach (CharacterData character in roster.Characters) // 전체 캐릭터 순회
        { // 보유 캐릭터 회복 대기 설정 시작
            if (!CharacterUnlockProgress.Shared.IsUnlocked(character)) // 보유 캐릭터 여부 확인
            { // 잠긴 캐릭터 제외 시작
                continue; // 다음 캐릭터 확인
            } // 잠긴 캐릭터 제외 종료
            results.RestoreSavedAllyState(character, 0, 50, 1); // 보유 캐릭터 사망 상태 설정
            if (owned > 0) // 회복 대기 누락 사례 확인
            { // 기존 회복 상태 설정 시작
                recovery.RestoreRecoveryState(character, 2); // 부활 대기 두 회 설정
            } // 기존 회복 상태 설정 종료
            owned++; // 보유 캐릭터 수 증가
            if (owned == 1) // 일부 캐릭터만 출전 불가한 상태 확인
            { // 전체 보유 상태 검사 시작
                Assert.IsFalse(wait.gameObject.activeSelf); // 다른 보유 캐릭터가 출전 가능하면 대기 차단 확인
            } // 전체 보유 상태 검사 종료
        } // 보유 캐릭터 회복 대기 설정 종료
        Assert.Greater(owned, 0); // 보유 캐릭터 존재 확인
        Assert.IsTrue(wait.gameObject.activeSelf); // 전원 출전 불가 시 대기 버튼 표시 확인
        wait.GetComponent<Button>().onClick.Invoke(); // 실제 거점 긴급 대기 한 번 선택
        Assert.IsTrue(wait.gameObject.activeSelf); // 여전히 출전 불가 시 재대기 가능 확인
        foreach (CharacterData character in roster.Characters) // 전체 캐릭터 순회
        { // 첫 긴급 대기 결과 확인 시작
            if (CharacterUnlockProgress.Shared.IsUnlocked(character)) // 보유 캐릭터 확인
            { // 부활 대기 확인 시작
                Assert.AreEqual(1, recovery.GetRemainingRecoveryExpeditions(character)); // 남은 부활 대기 한 회 확인
            } // 부활 대기 확인 종료
        } // 첫 긴급 대기 결과 확인 종료
        Assert.IsTrue(LobbySaveService.TryCapture(out string savedRecovery)); // 긴급 대기 후 거점 상태 저장 확인
        wait.GetComponent<Button>().onClick.Invoke(); // 실제 거점 긴급 대기 두 번째 선택
        Assert.IsFalse(wait.gameObject.activeSelf); // 출전 가능 캐릭터 회복 후 대기 제한 확인
        Assert.IsFalse(recovery.TryEmergencyWait(roster)); // 출전 가능 인원이 있으면 직접 대기 요청도 차단 확인
        Assert.IsTrue(LobbySaveService.LoadJson(savedRecovery)); // 중간 대기 상태 저장 복원 확인
        Assert.IsTrue(wait.gameObject.activeSelf); // 저장 복원 후 긴급 대기 버튼 재표시 확인
        wait.GetComponent<Button>().onClick.Invoke(); // 저장 복원 후 남은 긴급 대기 실행
        Assert.IsFalse(wait.gameObject.activeSelf); // 저장 복원 후 회복 완료 확인
        foreach (CharacterData character in roster.Characters) // 전체 캐릭터 순회
        { // 부활 완료 결과 확인 시작
            if (CharacterUnlockProgress.Shared.IsUnlocked(character)) // 보유 캐릭터 확인
            { // 캐릭터 체력 확인 시작
                Assert.IsTrue(recovery.CanDeploy(character)); // 회복된 캐릭터 출전 가능 확인
                Assert.IsTrue(results.TryGetSavedAllyHealth(character.CharacterId, out int health)); // 회복된 체력 조회 확인
                Assert.AreEqual(character.MaxHealth, health); // 부활 완료 최대 체력 확인
            } // 캐릭터 체력 확인 종료
        } // 부활 완료 결과 확인 종료
        Assert.AreEqual(100, resources.Gold); // 골드 보상과 소비 없음 확인
        Assert.AreEqual(20, resources.Screw); // 나사 보상과 소비 없음 확인
        Assert.AreEqual(30, resources.IronPlate); // 철판 보상과 소비 없음 확인
        Assert.AreEqual(40, resources.Wire); // 전선 보상과 소비 없음 확인
        Assert.AreEqual(previousLevel, progression.Level); // 성장 레벨 변화 없음 확인
        Assert.AreEqual(previousExperience, progression.CurrentExperience); // 경험치 보상 없음 확인
        Assert.AreEqual(previousAffinity, affinity.Affinity); // 호감도 보상 없음 확인
        Assert.AreEqual(previousCompletion, session.CompletionVersion); // 탐사 성공 횟수 변화 없음 확인
        Object.Destroy(canvasObject); // 임시 거점 화면 제거
        recovery.ResetRecoveryState(); // 테스트 회복 상태 정리
        results.ResetSavedPartyState(); // 테스트 캐릭터 상태 정리
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 긴급 대기 검사 종료
} // 전원 출전 불가 검사 종료
