using System; // 화면 준비 조건 사용
using System.Collections; // 게임 프레임 대기
using System.Collections.Generic; // 회복 완료 명단 기록
using System.Linq; // 편성 순서와 고유 번호 확인
using NUnit.Framework; // 전투와 저장 결과 검사
using UnityEngine; // 게임 객체와 해금 설정 조회
using UnityEngine.SceneManagement; // 실제 게임 씬 이동
using UnityEngine.TestTools; // 플레이 모드 검사
using UnityEngine.UI; // 실제 편성 버튼 조작
using Object = UnityEngine.Object; // 유니티 객체 검색
public sealed class RosterRecoveryFlowTests // 아르카나 전체 출전과 회복 흐름 검사
{ // 전체 명단 검사 시작
    [UnityTest] // 실제 씬 회귀 검사 지정
    [Timeout(300000)] // 여덟 탐사의 최대 검사 시간
    public IEnumerator AllCharactersFightDieRecoverAndReload() // 전체 캐릭터의 사망과 재출전 확인
    { // 전체 흐름 시작
        if (Application.companyName != "ProjectCVerification" || Application.productName != "ProjectCVerification") // 자동 저장 전용 복제본 확인
        { // 실제 사용자 저장 보호 시작
            Assert.Ignore("자동 저장을 분리한 ProjectCVerification 회사·제품 프로필에서 실행하세요."); // 원본 프로젝트 검사 생략
        } // 실제 사용자 저장 보호 종료
        yield return new EnterPlayMode(); // 실제 게임 실행
        const string unlockKey = "ProjectC.Progress.FinalExplorationCleared"; // 세계 해금 설정 이름
        bool hadUnlock = PlayerPrefs.HasKey(unlockKey); // 이전 해금 설정 유무
        int previousUnlock = PlayerPrefs.GetInt(unlockKey); // 이전 세계 해금 값
        try // 검증 프로필의 해금 설정 보존
        { // 명단 검사 시작
            CharacterUnlockProgress.Shared.RecordFinalClear(); // 최종 승리 이후의 명단 준비
            SceneManager.LoadScene("00_Boot"); // 게임 시작 씬 진입
            yield return WaitFor(() => SceneReady("10_MainMenu") && Find<MainMenuController>() != null, "메인 메뉴"); // 시스템과 메뉴 준비 대기
            Find<MainMenuController>().StartGame(); // 검증 계정에서 새 게임 시작
            yield return WaitFor(() => SceneReady("20_Lobby") && Find<LobbyLayoutView>() != null, "첫 거점"); // 거점 상태 초기화 대기
            CharacterData[] roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters.ToArray(); // 원본 명단 순서 보관
            CollectionAssert.AreEqual(Enumerable.Range(0, 22).Select(number => $"ARC_{number:D2}").ToArray(), roster.Select(member => member.CharacterId).ToArray()); // 스물두 고유 원본과 순서 확인
            int[] lastLoss = Enumerable.Repeat(-1, roster.Length).ToArray(); // 캐릭터별 최근 사망 탐사 번호
            int[] deaths = new int[roster.Length]; // 캐릭터별 예상 사망 횟수
            int[] mental = roster.Select(member => member.InitialMental).ToArray(); // 캐릭터별 예상 저장 정신력
            HashSet<string> recovered = new HashSet<string>(); // 회복 완료 후 편성 가능한 명단
            int groupCount = Mathf.CeilToInt(roster.Length / (float)PartyData.MaxPartySize); // 원본 명단을 나눈 여섯 편성
            for (int expedition = 0; expedition < groupCount + 2; expedition++) // 여섯 편성과 마지막 부활 대기 두 차례
            { // 한 번의 탐사 시작
                int first = expedition % groupCount * PartyData.MaxPartySize; // 현재 편성의 첫 캐릭터 위치
                int count = Mathf.Min(PartyData.MaxPartySize, roster.Length - first); // 마지막 두 명을 포함한 편성 인원
                PartyPresetStore.Reset(); // 새 조의 편성을 빈 슬롯부터 시작
                Click(Find<LobbyLayoutView>().transform, "ExpeditionButton"); // 거점에서 탐사 지역 선택
                Transform mapMenu = Find<ExpeditionMapView>().transform; // 실제 지역 선택 화면 조회
                Click(mapMenu, "MapOption_0"); // 기본 탐사 선택
                Click(mapMenu, "ConfirmButton"); // 파티 편성 화면 열기
                Transform formation = Find<PartyFormationView>().transform; // 실제 편성 화면 조회
                for (int index = first; index < first + count; index++) // 해당 조의 출전자 순회
                { // 파티원 선택 시작
                    Click(formation, "CharacterList/Viewport/Grid/Character_" + index); // 원본 명단의 프로필 선택
                } // 파티원 선택 종료
                string[] expectedIds = roster.Skip(first).Take(count).Select(member => member.CharacterId).ToArray(); // 선택한 원본 ID 목록
                CollectionAssert.AreEqual(expectedIds, PartyPresetStore.GetMembers(0), "출발 전 편성 순서"); // 실제 버튼으로 구성한 편성 확인
                string[] savedSlots = PartyPresetStore.GetSlots(0); // 두 명 편성의 빈 칸까지 보관
                Click(formation, "StartButton"); // 실제 편성으로 탐사 출발
                yield return WaitFor(() => SceneReady("30_Exploration") && Find<ExplorationEncounterView>() != null, "탐사 조우"); // 생성된 조우 대기
                Find<ExplorationEncounterView>().SendMessage("OnTriggerEnter2D", Find<ExplorationPlayerController>().GetComponent<Collider2D>()); // 플레이어 접촉으로 실제 조우 시작
                yield return WaitFor(() => SceneReady("40_Battle") && Find<BattleSceneSetup>() != null && Find<BattleSceneSetup>().IsInitialized, "전투"); // 전투 유닛과 덱 대기
                BattleSceneSetup battle = Find<BattleSceneSetup>(); // 실제 전투 관리자 조회
                CollectionAssert.AreEqual(expectedIds, battle.AllyUnits.Select(unit => unit.UnitId).ToArray(), "전투 명단과 편성 순서"); // 선택한 캐릭터와 실제 출전자 일치 확인
                foreach (BattleUnitRuntime ally in battle.AllyUnits) // 실제 출전자별 카드 연결 확인
                { // 카드 소유자 확인 시작
                    Assert.IsTrue(battle.BattleDeck.Cards.Any(card => card.OwnerUnit == ally), ally.UnitId); // 현재 전투 덱에 해당 아군 소유 카드 확인
                } // 카드 소유자 확인 종료
                foreach (BattleUnitRuntime ally in battle.AllyUnits) // 실제 전투 피해로 전멸 조건 준비
                { // 아군 피해 처리 시작
                    ally.TakeDamage(100000, BattleDamageType.Physical, battle.EnemyUnits[0]); // 적을 출처로 치명 피해 적용
                } // 아군 피해 처리 종료
                Assert.AreEqual(BattleResult.Defeat, battle.BattleTurn.Result); // 실제 전투 엔진의 패배 판정 확인
                for (int index = 0; index < count; index++) // 종료된 전투의 파티원 상태 기록
                { // 사망 기록 시작
                    lastLoss[first + index] = expedition; // 해당 캐릭터의 최근 사망 회차 기록
                    deaths[first + index]++; // 독립 예상 사망 횟수 증가
                    mental[first + index] = battle.AllyUnits[index].CurrentMental; // 전투 종료 시 표시할 정신력 보관
                } // 사망 기록 종료
                Click(Find<BattleResultView>().transform, "ConfirmButton"); // 패배 확인 후 탐사로 복귀
                yield return WaitFor(() => SceneReady("30_Exploration") && GameObject.Find("ExplorationResult") != null, "전멸 정산"); // 탐사 실패 화면 대기
                Assert.IsFalse(ExplorationSessionManager.Instance.IsExplorationSuccess); // 탐사 실패 정산 확인
                Click(GameObject.Find("ExplorationResult").transform, "ReturnButton"); // 거점 귀환과 자동 저장
                yield return WaitFor(() => SceneReady("20_Lobby") && Find<LobbyLayoutView>() != null, "거점 복귀"); // 귀환 자동 저장 완료 대기
                CheckRoster(roster, lastLoss, deaths, mental, expedition, recovered); // 실제 귀환 직후의 상태 검사
                BattleResultManager.Instance.ResetSavedPartyState(); // 메모리 내 체력 및 사망 기록 비우기
                CharacterRecoveryManager.Instance.ResetRecoveryState(); // 메모리 내 회복 상태 비우기
                PartyPresetStore.Reset(); // 메모리 내 편성 비우기
                Assert.IsTrue(LobbySaveService.Load(LobbySaveService.DefaultPath), "거점 자동 저장 파일 읽기"); // 귀환 시 생성된 실제 파일 복원
                CollectionAssert.AreEqual(savedSlots, PartyPresetStore.GetSlots(0), "저장 복원 후 빈 칸과 파티 순서"); // 슬롯 위치와 캐릭터 복원 확인
                CheckRoster(roster, lastLoss, deaths, mental, expedition, recovered); // 복원 후 전체 명단의 상태 재검사
            } // 한 번의 탐사 종료
            CollectionAssert.AreEquivalent(roster.Select(member => member.CharacterId).ToArray(), recovered, "회복 완료 후 출전 가능한 전체 명단"); // 스물두 명의 회복 완료 누락 검사
        } // 명단 검사 종료
        finally // 이전 해금 상태 복구
        { // 설정 복구 시작
            if (hadUnlock) // 이전 설정 존재 확인
            { // 이전 값 복구 시작
                PlayerPrefs.SetInt(unlockKey, previousUnlock); // 이전 세계 해금 값 복원
            } // 이전 값 복구 종료
            else // 이전 설정 미등록 상태
            { // 임시 설정 정리 시작
                PlayerPrefs.DeleteKey(unlockKey); // 검사에서 만든 해금 설정 제거
            } // 임시 설정 정리 종료
            PlayerPrefs.Save(); // 검증 프로필 설정 복구 반영
        } // 설정 복구 종료
        yield return new ExitPlayMode(); // 게임 실행 종료
    } // 전체 흐름 종료
    private static void CheckRoster(CharacterData[] roster, int[] lastLoss, int[] deaths, int[] mental, int expedition, HashSet<string> recovered) // 캐릭터별 귀환과 복원 결과 대조
    { // 출전 상태 확인 시작
        CharacterRecoveryManager recovery = CharacterRecoveryManager.Instance; // 실제 회복 상태 조회
        CharacterRecoveryDeploymentStatusProvider status = new CharacterRecoveryDeploymentStatusProvider(recovery); // 실제 편성과 동일한 출전 규칙
        for (int index = 0; index < roster.Length; index++) // 출전 및 미출전 캐릭터 전체 순회
        { // 개별 캐릭터 검사 시작
            CharacterData member = roster[index]; // 현재 원본 캐릭터 조회
            string context = $"{expedition + 1}번째 탐사 / {member.CharacterId}"; // 실패 원인에 표시할 회차와 캐릭터
            int remaining = lastLoss[index] < 0 ? 0 : Mathf.Max(0, 2 - (expedition - lastLoss[index])); // 사망 이후 끝난 다른 탐사 수로 남은 대기 계산
            Assert.AreEqual(remaining, recovery.GetRemainingRecoveryExpeditions(member), context); // 두 번의 탐사 뒤 부활하는지 확인
            Assert.IsTrue(BattleResultManager.Instance.TryGetSavedAllyState(member, out int health, out int currentMental, out int deathCount), context); // 해당 캐릭터의 영속 상태 조회
            Assert.AreEqual(remaining > 0 ? 0 : member.MaxHealth, health, context + " 체력"); // 대기 중 사망과 완료 후 최대 체력 확인
            Assert.AreEqual(mental[index], currentMental, context + " 정신력"); // 전투 밖과 파일 복원 뒤 정신력 보존 확인
            Assert.AreEqual(deaths[index], deathCount, context + " 사망 횟수"); // 중복 사망 집계 및 다른 캐릭터 오염 확인
            PartySelection probe = new PartySelection(status); // 실제 출전 판정이 적용되는 새 편성
            Assert.AreEqual(remaining == 0, probe.TryAdd(member), context + " 재편성"); // 회복 대기 중 차단과 완료 후 허용 확인
            if (lastLoss[index] >= 0 && remaining == 0) // 실제 사망 후 회복한 캐릭터 확인
            { // 회복 완료 기록 시작
                recovered.Add(member.CharacterId); // 재편성까지 통과한 캐릭터 기록
            } // 회복 완료 기록 종료
        } // 개별 캐릭터 검사 종료
    } // 출전 상태 확인 종료
    private static T Find<T>() where T : Object // 현재 씬의 활성 요소 검색
    { // 요소 검색 시작
        return Object.FindFirstObjectByType<T>(); // 첫 활성 요소 반환
    } // 요소 검색 종료
    private static bool SceneReady(string scene) // 로딩 화면이 닫힌 실제 씬 확인
    { // 씬 확인 시작
        return SceneManager.GetActiveScene().name == scene && SceneFlowManager.Instance != null && !SceneFlowManager.Instance.IsLoadingScene; // 현재 씬과 로딩 완료 판정
    } // 씬 확인 종료
    private static IEnumerator WaitFor(Func<bool> ready, string stage) // 시간 제한을 둔 화면 대기
    { // 화면 대기 시작
        float deadline = Time.realtimeSinceStartup + 30f; // 화면별 최대 대기 시각
        while (!ready() && Time.realtimeSinceStartup < deadline) // 화면 상태와 남은 시간 확인
        { // 프레임 대기 시작
            yield return null; // 다음 프레임 대기
        } // 프레임 대기 종료
        Assert.IsTrue(ready(), stage); // 대기한 화면 도착 확인
        yield return null; // 씬 시작 및 버튼 연결 대기
    } // 화면 대기 종료
    private static void Click(Transform parent, string path) // 실제 게임 버튼 선택
    { // 버튼 입력 시작
        Button button = parent.Find(path).GetComponent<Button>(); // 지정 버튼 조회
        Assert.IsTrue(button.isActiveAndEnabled && button.interactable, path); // 활성 상태와 입력 가능 확인
        button.onClick.Invoke(); // 실제 게임 버튼 실행
    } // 버튼 입력 종료
} // 전체 명단 검사 종료
