using System.Collections; // 플레이 모드 코루틴 사용
using System.Reflection; // 조우 버전 설정 검사 사용
using NUnit.Framework; // 저장과 조우 호환성 검증 사용
using UnityEngine; // 실제 씬과 조우 리소스 사용
using UnityEngine.SceneManagement; // 실제 탐사 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
public sealed class WinterfallEncounterCompatibilityTests // 이전 저장과 새 적 조우 호환성 검사
{ // 조우 호환성 검사 시작
    [Test] // 조우 버전 필터 검사 지정
    public void EncounterContentVersion_ExcludesNewEnemiesFromLegacyPool() // 이전 탐사에서 새 조우 제외 확인
    { // 조우 버전 필터 검사 시작
        EncounterData encounter = ScriptableObject.CreateInstance<EncounterData>(); // 임시 조우 원본 생성
        typeof(EncounterData).GetField("minimumEncounterPoolVersion", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(encounter, 1); // 새 조우의 최소 버전 설정
        Assert.IsFalse(encounter.IsAvailableForPoolVersion(0)); // 이전 저장 조우 목록에서 제외 확인
        Assert.IsTrue(encounter.IsAvailableForPoolVersion(1)); // 새 탐사 조우 목록에서 포함 확인
        Object.DestroyImmediate(encounter); // 임시 조우 원본 정리
    } // 조우 버전 필터 검사 종료
    [UnityTest] // 기존 저장 재개와 재저장 검사 지정
    public IEnumerator LegacySave_KeepsLegacyEncounterPoolAfterResaving() // 기존 저장의 조우 버전 유지 확인
    { // 실제 저장 호환성 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string path = System.IO.Path.Combine(Application.temporaryCachePath, System.Guid.NewGuid().ToString("N") + ".json"); // 독립 저장 경로 생성
        try // 테스트 저장 파일 정리 보장
        { // 이전 저장 복원 검사 시작
            SceneManager.LoadScene("00_Boot"); // 시작 씬 열기
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<MainMenuController>() == null;) // 시작 메뉴 대기
            { // 메뉴 대기 시작
                yield return null; // 다음 프레임 대기
            } // 메뉴 대기 종료
            Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 로비 진입
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 로비 대기
            { // 로비 대기 시작
                yield return null; // 다음 프레임 대기
            } // 로비 대기 종료
            PartyDepartureTestSupport.StartExploration(0); // 실제 탐사 시작
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 탐사 플레이어 대기
            { // 플레이어 대기 시작
                yield return null; // 다음 프레임 대기
            } // 플레이어 대기 종료
            ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 탐사 플레이어 조회
            Assert.IsNotNull(player); // 탐사 플레이어 존재 확인
            yield return null; // 탐사 상태 초기화 대기
            Assert.AreEqual(ExplorationSessionManager.LatestEncounterPoolVersion, ExplorationSessionManager.Instance.EncounterPoolVersion); // 새 탐사의 최신 조우 버전 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 새 탐사 저장 확인
            string json = System.IO.File.ReadAllText(path); // 실제 탐사 저장 읽기
            string previous = System.Text.RegularExpressions.Regex.Replace(json, "(\\\"encounterPoolVersion\\\"\\s*:\\s*)" + ExplorationSessionManager.LatestEncounterPoolVersion, "${1}1"); // 144일차 조우 버전 저장 생성
            Assert.AreNotEqual(json, previous); // 이전 버전 저장 변환 확인
            System.IO.File.WriteAllText(path, previous); // 독립 이전 버전 저장 기록
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 144일차 저장 복원 확인
            Assert.AreEqual(1, ExplorationSessionManager.Instance.EncounterPoolVersion); // 144일차 조우 버전 유지 확인
            Assert.IsTrue(Resources.Load<EncounterData>("Encounters/Encounter_Test_C").IsAvailableForPoolVersion(1)); // 이전 저장의 임시 보스 유지 확인
            Assert.IsFalse(Resources.Load<EncounterData>("Encounters/Encounter_WF_AshstormCore").IsAvailableForPoolVersion(1)); // 이전 저장의 새 보스 제외 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 이전 버전 탐사 재저장 확인
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 재저장한 이전 버전 탐사 복원 확인
            Assert.AreEqual(1, ExplorationSessionManager.Instance.EncounterPoolVersion); // 재저장 후 이전 조우 버전 유지 확인
            string legacy = System.Text.RegularExpressions.Regex.Replace(json, "\\\"encounterPoolVersion\\\"\\s*:\\s*" + ExplorationSessionManager.LatestEncounterPoolVersion + "\\s*,", string.Empty); // 새 필드가 없는 이전 저장 형식 생성
            Assert.AreNotEqual(json, legacy); // 조우 버전 필드 제거 확인
            System.IO.File.WriteAllText(path, legacy); // 독립 이전 저장 기록
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 이전 저장 복원 확인
            Assert.AreEqual(0, ExplorationSessionManager.Instance.EncounterPoolVersion); // 이전 저장의 이전 조우 버전 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 이전 탐사 재저장 확인
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 재저장한 이전 탐사 복원 확인
            Assert.AreEqual(0, ExplorationSessionManager.Instance.EncounterPoolVersion); // 재저장 후에도 이전 조우 버전 유지 확인
            SceneManager.LoadScene("30_Exploration"); // 복원된 시드로 실제 탐사 씬 재생성
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<ExplorationEncounterView>() == null;) // 복원된 조우 대기
            { // 조우 대기 시작
                yield return null; // 다음 프레임 대기
            } // 조우 대기 종료
            yield return null; // 방 역할별 조우 배치 완료 대기
            ExplorationEncounterView[] views = Object.FindObjectsByType<ExplorationEncounterView>(FindObjectsSortMode.None); // 복원된 조우 화면 조회
            Assert.Greater(views.Length, 0); // 복원된 탐사에 조우 존재 확인
            FieldInfo dataField = typeof(ExplorationEncounterView).GetField("encounterData", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 조우 원본 필드 조회
            foreach (ExplorationEncounterView view in views) // 복원된 조우 순회
            { // 이전 조우 구성 확인 시작
                EncounterData encounter = (EncounterData)dataField.GetValue(view); // 실제 조우 데이터 조회
                Assert.IsNotNull(encounter); // 조우 원본 존재 확인
                Assert.IsTrue(encounter.IsAvailableForPoolVersion(0), encounter.EncounterId); // 모든 조우가 이전 목록에 속함 확인
            } // 이전 조우 구성 확인 종료
        } // 이전 저장 복원 검사 종료
        finally // 테스트 저장 파일 정리
        { // 저장 파일 정리 시작
            ExplorationSaveService.Remove(path); // 독립 저장과 복구본 제거
        } // 저장 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 실제 저장 호환성 검사 종료
} // 조우 호환성 검사 종료
