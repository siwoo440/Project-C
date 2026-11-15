using System; // 고유 테스트 파일 이름 사용
using System.Collections; // 플레이 모드 대기 사용
using System.IO; // 독립 저장 파일 검사 사용
using System.Text.RegularExpressions; // 저장 실패 로그 검사 사용
using NUnit.Framework; // 자동 저장 동작 검증 사용
using UnityEngine; // 캐릭터와 위치 사용
using UnityEngine.TestTools; // 플레이 모드 테스트 사용

public sealed class ExplorationAutoSaveTests // 전투 전후 자동 저장 검증
{ // 자동 저장 테스트 시작
    [UnityTest] // 플레이 모드 테스트 지정
    public IEnumerator Checkpoints_SaveBeforeBattleAndAfterRewardWithoutDuplication() // 전투 전후 저장과 보상 복원 확인
    { // 전투 자동 저장 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string root = Path.Combine(Application.temporaryCachePath, "checkpoint-" + Guid.NewGuid().ToString("N")); // 독립 테스트 파일 경로 생성
        string runPath = root + ".run"; // 독립 탐사 저장 경로 생성
        string lobbyPath = root + ".lobby"; // 독립 거점 저장 경로 생성
        string blockingPath = root + ".block"; // 저장 실패 재현용 파일 경로 생성
        try // 테스트 파일 정리 보장
        { // 전투 자동 저장 검증 시작
            CharacterData character = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]; // 실제 출전 캐릭터 조회
            PartySelection selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 출전 파티 선택 생성
            Assert.IsTrue(selection.TryAdd(character)); // 실제 캐릭터 선택 확인
            Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 출전 파티와 덱 준비
            RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 탐사 덱 초기화
            ExplorationSessionManager session = ExplorationSessionManager.EnsureInstance(); // 탐사 상태 관리자 준비
            session.ResetExploration(); // 독립 탐사 상태 준비
            session.SetCurrentFloorSeed(123456); // 고정 탐사 맵 시드 설정
            PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 실제 자원 관리자 준비
            resources.ResetResources(); // 독립 보상 자원 초기화
            Vector3 beforePosition = new Vector3(2f, 3f, 0f); // 전투 진입 위치 설정
            Assert.IsTrue(ExplorationAutoSave.SaveBeforeBattle(beforePosition, runPath)); // 전투 전 탐사 상태 자동 저장 확인
            Assert.IsTrue(ExplorationSaveService.CanLoad(runPath)); // 전투 전 저장 유효성 확인
            EncounterData encounter = Resources.Load<EncounterData>("Encounters/Encounter_Test_A"); // 실제 전투 조우 원본 조회
            Assert.IsTrue(session.BeginEncounter("auto-test-encounter", encounter, beforePosition, Vector3.zero)); // 테스트 전투 시작
            Assert.IsFalse(ExplorationAutoSave.SaveBeforeBattle(beforePosition, runPath)); // 진행 중 전투의 기존 저장 보존 확인
            BattleUnitRuntime ally = BattleUnitRuntime.CreateAlly(character); // 생존 캐릭터 전투 상태 생성
            BattleResultData victory = new BattleResultData(BattleResult.Victory, BattleType.Normal, 1, new[] { ally }, new string[0]); // 실제 승리 결과 생성
            session.ResolveBattleResult(victory); // 조우 클리어와 보상 반영
            int rewardedGold = resources.Gold; // 지급된 보상 골드 보관
            Assert.Greater(rewardedGold, 0); // 승리 보상 지급 확인
            Assert.IsTrue(ExplorationAutoSave.SaveAfterBattle(new Vector3(4f, 5f, 0f), runPath, lobbyPath)); // 보상 처리 후 탐사 저장 확인
            session.ResetExploration(); // 기존 탐사 상태 제거
            resources.ResetResources(); // 기존 보상 자원 제거
            Assert.IsTrue(ExplorationSaveService.Load(runPath)); // 자동 저장된 탐사 복원 확인
            Assert.IsTrue(session.IsEncounterCleared("auto-test-encounter")); // 저장된 조우 클리어 상태 확인
            Assert.AreEqual(rewardedGold, resources.Gold); // 저장된 보상 자원 확인
            session.ResolveBattleResult(victory); // 이미 처리된 전투 결과 재전달
            Assert.AreEqual(rewardedGold, resources.Gold); // 승리 보상 중복 지급 방지 확인
            Assert.IsTrue(session.CompleteExplorationFailure("테스트 종료")); // 탐사 종료 상태 설정
            File.WriteAllText(blockingPath, "파일"); // 거점 저장 폴더 대신 파일 생성
            LogAssert.Expect(LogType.Warning, new Regex("\\[SaveFile\\] 저장 실패")); // 예상된 거점 저장 실패 로그 확인
            Assert.IsFalse(ExplorationAutoSave.SaveAfterBattle(Vector3.zero, runPath, Path.Combine(blockingPath, "save.json"))); // 실패한 거점 저장 결과 확인
            Assert.IsTrue(File.Exists(runPath)); // 거점 저장 실패 시 기존 탐사 저장 보존 확인
            Assert.IsTrue(ExplorationAutoSave.SaveAfterBattle(Vector3.zero, runPath, lobbyPath)); // 종료 후 거점 저장 확인
            Assert.IsTrue(LobbySaveService.CanLoad(lobbyPath)); // 종료 거점 저장 유효성 확인
            Assert.IsFalse(File.Exists(runPath)); // 정상 거점 저장 후 이전 탐사 저장 정리 확인
        } // 전투 자동 저장 검증 종료
        finally // 테스트 파일 정리
        { // 파일 정리 시작
            if (File.Exists(runPath)) // 탐사 테스트 파일 존재 확인
            { // 탐사 파일 정리 시작
                File.Delete(runPath); // 독립 탐사 파일 삭제
            } // 탐사 파일 정리 종료
            if (File.Exists(lobbyPath)) // 거점 테스트 파일 존재 확인
            { // 거점 파일 정리 시작
                File.Delete(lobbyPath); // 독립 거점 파일 삭제
            } // 거점 파일 정리 종료
            if (File.Exists(blockingPath)) // 저장 실패 재현 파일 존재 확인
            { // 오류 재현 파일 정리 시작
                File.Delete(blockingPath); // 독립 오류 재현 파일 삭제
            } // 오류 재현 파일 정리 종료
        } // 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 전투 자동 저장 검사 종료
} // 자동 저장 테스트 종료
