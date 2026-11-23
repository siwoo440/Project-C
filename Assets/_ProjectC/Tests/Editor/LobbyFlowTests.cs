using System; // 고유 테스트 파일 이름 사용
using System.Collections; // 플레이 모드 대기 사용
using System.IO; // 독립 저장 파일 검사 사용
using NUnit.Framework; // 거점 흐름 검증 사용
using UnityEngine; // 게임 상태와 씬 객체 사용
using UnityEngine.SceneManagement; // 실제 거점 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 테스트 사용
using UnityEngine.UI; // 실제 복귀 버튼 사용
using TMPro; // 결과 안내 문구 확인 사용

public sealed class LobbyFlowTests // 거점 사망과 복귀 흐름 검증
{ // 거점 흐름 테스트 시작
    [UnityTest] // 플레이 모드 테스트 지정
    public IEnumerator FailureCheckpoint_RegistersRecoveryBeforeSavingOnlyOnce() // 종료 직후 회복 저장과 중복 방지 확인
    { // 실패 저장 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string root = Path.Combine(Application.temporaryCachePath, "lobby-flow-" + Guid.NewGuid().ToString("N")); // 독립 저장 경로 생성
        string runPath = root + ".run"; // 독립 탐사 저장 경로 생성
        string lobbyPath = root + ".lobby"; // 독립 거점 저장 경로 생성
        try // 테스트 파일 정리 보장
        { // 실패 저장 검증 시작
            CharacterData character = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]; // 실제 캐릭터 조회
            CharacterRecoveryManager recovery = CharacterRecoveryManager.EnsureInstance(); // 회복 관리자 준비
            recovery.ResetRecoveryState(); // 이전 회복 상태 초기화
            BattleResultManager result = BattleResultManager.EnsureInstance(); // 전투 결과 관리자 준비
            result.ResetSavedPartyState(); // 이전 캐릭터 상태 초기화
            PartySelection selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(recovery)); // 출전 가능 상태 기준 파티 선택 생성
            Assert.IsTrue(selection.TryAdd(character)); // 실제 캐릭터 선택 확인
            Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 선택 파티와 덱 준비
            Assert.IsTrue(result.RegisterParty(PartyLoadoutRuntime.Active.Party)); // 실제 탐사 파티 등록 확인
            result.RestoreSavedAllyState(character, 0, 50, 1); // 사망한 캐릭터 상태 재현
            ExplorationSessionManager session = ExplorationSessionManager.EnsureInstance(); // 탐사 상태 관리자 준비
            session.ResetExploration(); // 독립 탐사 상태 초기화
            Assert.IsTrue(session.CompleteExplorationFailure("검증용 전멸")); // 탐사 실패 처리
            Assert.IsTrue(ExplorationAutoSave.SaveAfterBattle(Vector3.zero, runPath, lobbyPath)); // 같은 프레임 거점 자동 저장
            Assert.AreEqual(2, recovery.GetRemainingRecoveryExpeditions(character)); // 저장 전 신규 사망 회복 등록 확인
            Assert.IsTrue(ExplorationAutoSave.SaveAfterBattle(Vector3.zero, runPath, lobbyPath)); // 같은 탐사 결과 재저장
            Assert.AreEqual(2, recovery.GetRemainingRecoveryExpeditions(character)); // 같은 탐사에서 회복 횟수 중복 차감 방지
            yield return null; // 회복 관리자 자동 처리 기회 제공
            Assert.AreEqual(2, recovery.GetRemainingRecoveryExpeditions(character)); // 프레임 갱신의 회복 중복 처리 방지
            recovery.ResetRecoveryState(); // 현재 회복 상태 제거
            Assert.IsTrue(LobbySaveService.Load(lobbyPath)); // 자동 저장된 거점 상태 복원
            Assert.AreEqual(2, recovery.GetRemainingRecoveryExpeditions(character)); // 저장된 사망 회복 횟수 복원 확인
            CharacterData alternate = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[1]; // 다른 출전 캐릭터 조회
            PartySelection next = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(recovery)); // 회복 상태 기준 다음 파티 선택 생성
            Assert.IsFalse(next.TryAdd(character)); // 회복 중 캐릭터 출전 차단 확인
            Assert.IsTrue(next.TryAdd(alternate)); // 다른 캐릭터 출전 선택 확인
            Assert.IsTrue(PartyLoadoutRuntime.Prepare(next)); // 다른 캐릭터의 탐사 파티 준비
            Assert.IsTrue(result.RegisterParty(PartyLoadoutRuntime.Active.Party)); // 다음 탐사 파티 등록
            session.ResetExploration(); // 다음 탐사 시작 상태 설정
            Assert.IsTrue(session.CompleteExplorationSuccess()); // 다음 탐사 종료 처리
            Assert.IsTrue(recovery.EnsureCompletedExplorationProcessed()); // 첫 회복 진행 반영
            Assert.AreEqual(1, recovery.GetRemainingRecoveryExpeditions(character)); // 탐사 한 번의 회복 진행 확인
            session.ResetExploration(); // 두 번째 탐사 시작 상태 설정
            Assert.IsTrue(session.CompleteExplorationSuccess()); // 두 번째 탐사 종료 처리
            Assert.IsTrue(recovery.EnsureCompletedExplorationProcessed()); // 두 번째 회복 진행 반영
            Assert.IsFalse(recovery.IsRecovering(character)); // 회복 완료 확인
            Assert.IsTrue(next.TryAdd(character)); // 회복 완료 캐릭터 재출전 확인
        } // 실패 저장 검증 종료
        finally // 테스트 파일 정리
        { // 독립 저장 파일 정리 시작
            if (File.Exists(runPath)) // 탐사 원본 존재 확인
            { // 탐사 원본 정리 시작
                File.Delete(runPath); // 독립 탐사 원본 삭제
            } // 탐사 원본 정리 종료
            if (File.Exists(runPath + ".bak")) // 탐사 복구본 존재 확인
            { // 탐사 복구본 정리 시작
                File.Delete(runPath + ".bak"); // 독립 탐사 복구본 삭제
            } // 탐사 복구본 정리 종료
            if (File.Exists(lobbyPath)) // 거점 원본 존재 확인
            { // 거점 원본 정리 시작
                File.Delete(lobbyPath); // 독립 거점 원본 삭제
            } // 거점 원본 정리 종료
            if (File.Exists(lobbyPath + ".bak")) // 거점 복구본 존재 확인
            { // 거점 복구본 정리 시작
                File.Delete(lobbyPath + ".bak"); // 독립 거점 복구본 삭제
            } // 거점 복구본 정리 종료
        } // 독립 저장 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실패 저장 검사 종료
    [UnityTest] // 플레이 모드 테스트 지정
    public IEnumerator FailedExpedition_ReturnsToLobbyAndBlocksRecoveringPartyMember() // 탐사 실패 후 거점 복귀 확인
    { // 거점 복귀 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        SceneManager.LoadScene("00_Boot"); // 실제 부트 씬 시작
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && UnityEngine.Object.FindFirstObjectByType<MainMenuController>() == null;) // 메인 메뉴 준비 대기
        { // 메인 메뉴 대기 시작
            yield return null; // 다음 프레임 대기
        } // 메인 메뉴 대기 종료
        UnityEngine.Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 실제 새 게임 시작
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && UnityEngine.Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 거점 준비 대기
        { // 거점 대기 시작
            yield return null; // 다음 프레임 대기
        } // 거점 대기 종료
        PlayerResourceManager.EnsureInstance().AddResources(9999, 9999, 9999, 9999); // 설비 강화 자원 지급
        Assert.IsTrue(FacilityUpgradeManager.EnsureInstance().TryUpgrade(FacilityType.PowerSupply)); // 거점 설비 강화 확인
        PartyDepartureTestSupport.StartExploration(0); // 실제 출전 파티와 탐사 시작
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && UnityEngine.Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 탐사 씬 준비 대기
        { // 탐사 대기 시작
            yield return null; // 다음 프레임 대기
        } // 탐사 대기 종료
        CharacterData character = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]; // 출전 캐릭터 조회
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && UnityEngine.Object.FindFirstObjectByType<ExplorationHazardRuntime>() == null;) // 환경 피해 관리자 준비 대기
        { // 환경 관리자 대기 시작
            yield return null; // 다음 프레임 대기
        } // 환경 관리자 대기 종료
        BattleResultManager.EnsureInstance().RestoreSavedAllyState(character, 1, 50, 0); // 환경 피해로 사망할 체력 설정
        Assert.IsTrue(ExplorationAutoSave.SaveBeforeBattle(Vector3.zero)); // 종료 전 탐사 자동 저장 생성
        UnityEngine.Object.FindFirstObjectByType<ExplorationHazardRuntime>().SendMessage("ApplyHazardPulse", 3); // 실제 환경 피해 전멸 처리
        Assert.IsTrue(ExplorationSessionManager.EnsureInstance().IsExplorationCompleted); // 환경 피해 탐사 실패 확인
        Assert.IsFalse(File.Exists(ExplorationSaveService.DefaultPath)); // 전멸 후 이전 탐사 저장 정리 확인
        CharacterRecoveryManager.EnsureInstance().ResetRecoveryState(); // 자동 저장의 회복 정보 복원 검증 준비
        Assert.IsTrue(LobbySaveService.Load(LobbySaveService.DefaultPath)); // 전멸 직후 자동 저장된 거점 상태 복원
        Assert.AreEqual(2, CharacterRecoveryManager.Instance.GetRemainingRecoveryExpeditions(character)); // 전멸 직후 저장된 회복 횟수 확인
        yield return null; // 사망 회복 등록 대기
        Assert.IsTrue(CharacterRecoveryManager.EnsureInstance().IsRecovering(character)); // 사망 캐릭터 회복 등록 확인
        GameObject panel = GameObject.Find("ExplorationResult"); // 탐사 결과 화면 조회
        Assert.IsNotNull(panel); // 탐사 실패 결과 화면 존재 확인
        Assert.AreEqual("탐사 실패", panel.transform.Find("Title").GetComponent<TMP_Text>().text); // 실제 실패 결과 제목 확인
        panel.transform.Find("ReturnButton").GetComponent<Button>().onClick.Invoke(); // 실제 결과 화면 거점 복귀 버튼 선택
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && SceneManager.GetActiveScene().name != "20_Lobby";) // 거점 복귀 대기
        { // 거점 복귀 대기 시작
            yield return null; // 다음 프레임 대기
        } // 거점 복귀 대기 종료
        Assert.AreEqual("20_Lobby", SceneManager.GetActiveScene().name); // 전멸 후 거점 복귀 확인
        Assert.AreEqual(1, FacilityUpgradeManager.Instance.GetLevel(FacilityType.PowerSupply)); // 거점 설비 강화 유지 확인
        PartySelection nextParty = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.Instance)); // 회복 상태 기준 재출발 파티 생성
        Assert.IsFalse(nextParty.TryAdd(character)); // 회복 중 캐릭터 편성 차단 확인
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && UnityEngine.Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 거점 화면 초기화 대기
        { // 거점 화면 대기 시작
            yield return null; // 다음 프레임 대기
        } // 거점 화면 대기 종료
        UnityEngine.Object.FindFirstObjectByType<LobbyLayoutView>().transform.Find("ExpeditionButton").GetComponent<Button>().onClick.Invoke(); // 실제 거점 탐사 버튼 선택
        GameObject map = GameObject.Find("MapSelection"); // 맵 선택 화면 조회
        map.transform.Find("MapOption_0").GetComponent<Button>().onClick.Invoke(); // 탐사 지역 선택
        map.transform.Find("ConfirmButton").GetComponent<Button>().onClick.Invoke(); // 파티 편성 화면 열기
        GameObject formation = GameObject.Find("FormationSetup"); // 실제 파티 편성 화면 조회
        Transform grid = formation.transform.Find("CharacterList/Viewport/Grid"); // 편성 캐릭터 목록 조회
        Assert.IsFalse(grid.Find("Character_0").GetComponent<Button>().interactable); // 회복 중 캐릭터 버튼 비활성 확인
        Assert.IsTrue(grid.Find("Character_1").GetComponent<Button>().interactable); // 생존 캐릭터 선택 가능 확인
        grid.Find("Character_1").GetComponent<Button>().onClick.Invoke(); // 다른 생존 캐릭터 편성
        Assert.IsTrue(formation.transform.Find("StartButton").GetComponent<Button>().interactable); // 새 파티 출발 가능 확인
        formation.transform.Find("StartButton").GetComponent<Button>().onClick.Invoke(); // 실제 탐사 재출발 버튼 선택
        for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && SceneManager.GetActiveScene().name != "30_Exploration";) // 재출발 탐사 씬 대기
        { // 재출발 대기 시작
            yield return null; // 다음 프레임 대기
        } // 재출발 대기 종료
        Assert.AreEqual("30_Exploration", SceneManager.GetActiveScene().name); // 실제 거점 재출발 완료 확인
        Assert.AreEqual("ARC_01", PartyLoadoutRuntime.Active.Party.Members[0].CharacterId); // 생존 캐릭터 재출발 편성 확인
        Assert.IsTrue(ExplorationSessionManager.EnsureInstance().CompleteExplorationSuccess()); // 재출발 탐사 성공 상태 설정
        yield return null; // 성공 결과 화면 표시 대기
        GameObject success = GameObject.Find("ExplorationResult"); // 성공 결과 화면 조회
        Assert.IsNotNull(success); // 탐사 성공 결과 화면 존재 확인
        Assert.AreEqual("탐사 성공", success.transform.Find("Title").GetComponent<TMP_Text>().text); // 성공 결과 제목 확인
        StringAssert.Contains("완료 층", success.transform.Find("Detail").GetComponent<TMP_Text>().text); // 기존 탐사 성공 정산 정보 확인
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 거점 복귀 검사 종료
} // 거점 흐름 테스트 종료
