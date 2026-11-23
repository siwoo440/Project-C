using System; // 독립 저장 파일 이름 사용
using System.Collections; // 플레이 모드 대기 사용
using System.IO; // 독립 저장 파일 정리 사용
using System.Text.RegularExpressions; // 저장 재화 오류와 이전 형식 재현 사용
using NUnit.Framework; // 전멸 재화 규칙 검증 사용
using UnityEngine; // 게임 객체와 위치 사용
using UnityEngine.SceneManagement; // 실제 씬 이동 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 실제 탐사 출발 버튼 사용
using Object = UnityEngine.Object; // 게임 객체 이름 충돌 방지

public sealed class ExplorationResourceLossTests // 탐사 재화와 전멸 통합 검사
{ // 재화 검사 시작
    [UnityTest] // 플레이 모드 검사 지정
    public IEnumerator Failure_AfterSavingLosesGainedResourcesAndKeepsExistingResources() // 저장 복원 후 탐사 획득분만 손실 확인
    { // 전멸 재화 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string path = Path.Combine(Application.temporaryCachePath, "run-resource-" + Guid.NewGuid().ToString("N") + ".json"); // 독립 탐사 저장 경로
        try // 저장 파일 정리 보장
        { // 재화 통합 검사 시작
            SceneManager.LoadScene("00_Boot"); // 실제 시작 씬 진입
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<MainMenuController>() == null;) // 시작 메뉴 대기
            { // 시작 메뉴 대기 시작
                yield return null; // 다음 프레임 대기
            } // 시작 메뉴 대기 종료
            Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>()); // 시작 메뉴 존재 확인
            Object.FindFirstObjectByType<MainMenuController>().StartGame(); // 새 게임 시작
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<LobbyLayoutView>() == null;) // 거점 초기화 대기
            { // 거점 초기화 대기 시작
                yield return null; // 다음 프레임 대기
            } // 거점 초기화 대기 종료
            PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 실제 자원 관리자 조회
            resources.ResetResources(); // 독립 자원 상태 초기화
            resources.AddResources(100, 20, 20, 20); // 탐사 전 영구 보유 자원 설정
            Object.FindFirstObjectByType<LobbyLayoutView>().transform.Find("ExpeditionButton").GetComponent<Button>().onClick.Invoke(); // 거점 탐사 버튼 선택
            GameObject map = GameObject.Find("MapSelection"); // 지역 선택 화면 조회
            Assert.IsNotNull(map); // 지역 선택 화면 확인
            map.transform.Find("MapOption_0").GetComponent<Button>().onClick.Invoke(); // 첫 지역 선택
            map.transform.Find("ConfirmButton").GetComponent<Button>().onClick.Invoke(); // 편성 화면 열기
            GameObject formation = GameObject.Find("FormationSetup"); // 실제 편성 화면 조회
            Assert.IsNotNull(formation); // 편성 화면 존재 확인
            formation.transform.Find("CharacterList/Viewport/Grid/Character_0").GetComponent<Button>().onClick.Invoke(); // 출전 캐릭터 선택
            formation.transform.Find("StartButton").GetComponent<Button>().onClick.Invoke(); // 실제 탐사 출발
            for (float deadline = Time.realtimeSinceStartup + 30f; Time.realtimeSinceStartup < deadline && Object.FindFirstObjectByType<ExplorationPlayerController>() == null;) // 탐사 플레이어 준비 대기
            { // 탐사 준비 대기 시작
                yield return null; // 다음 프레임 대기
            } // 탐사 준비 대기 종료
            ExplorationPlayerController player = Object.FindFirstObjectByType<ExplorationPlayerController>(); // 실제 탐사 플레이어 조회
            Assert.IsNotNull(player); // 탐사 플레이어 존재 확인
            resources.AddResources(60, 8, 9, 10); // 탐사 중 재화 획득 재현
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 획득 상태 탐사 저장
            string savedJson = File.ReadAllText(path); // 정상 탐사 저장 내용 보관
            string corruptJson = Regex.Replace(savedJson, "\"temporaryGold\":\\d+", "\"temporaryGold\":999999"); // 전체 잔액을 넘는 획득분 재현
            Assert.AreNotEqual(savedJson, corruptJson); // 손상 재화 내용 생성 확인
            Assert.IsFalse(ExplorationSaveService.CanLoadJson(corruptJson)); // 잘못된 획득분 복원 차단 확인
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 획득 상태 탐사 복원
            Assert.AreEqual(160, resources.Gold); // 복원된 전체 골드 확인
            Assert.IsTrue(ExplorationSessionManager.Instance.CompleteExplorationFailure("재화 손실 검사")); // 탐사 전멸 처리
            Assert.AreEqual(100, resources.Gold); // 기존 골드만 보존 확인
            Assert.AreEqual(20, resources.Screw); // 기존 나사만 보존 확인
            Assert.AreEqual(20, resources.IronPlate); // 기존 철판만 보존 확인
            Assert.AreEqual(20, resources.Wire); // 기존 전선만 보존 확인
            Assert.IsFalse(ExplorationSessionManager.Instance.CompleteExplorationFailure("재화 손실 재처리")); // 전멸 중복 처리 차단 확인
            Assert.AreEqual(100, resources.Gold); // 중복 호출 후 기존 골드 유지 확인
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 기존 획득 상태 재복원
            Assert.IsTrue(resources.TrySpend(80, 0, 0, 0)); // 기존 골드부터 80 소비
            Assert.IsTrue(ExplorationSaveService.Save(path, player.transform.position)); // 소비 후 재화 구분 저장
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 소비 후 재화 구분 복원
            Assert.AreEqual(80, resources.Gold); // 소비 후 총 골드 복원 확인
            Assert.IsTrue(ExplorationSessionManager.Instance.CompleteExplorationFailure("소비 후 전멸 검사")); // 저장 복원 뒤 전멸 처리
            Assert.AreEqual(20, resources.Gold); // 사용자 확인 기존 골드 우선 소비 결과 확인
            string legacyJson = savedJson.Replace("\"hasResourceLedger\":true", "\"hasResourceLedger\":false"); // 획득 출처가 없는 이전 저장 재현
            Assert.AreNotEqual(savedJson, legacyJson); // 이전 저장 형식 재현 확인
            File.WriteAllText(path, legacyJson); // 독립 이전 형식 저장 생성
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 이전 탐사 저장 복원 확인
            Assert.AreEqual(160, resources.Gold); // 출처를 모르는 기존 잔액 보존 확인
            resources.AddResources(5, 0, 0, 0); // 이전 저장 복원 후 새 골드 획득
            Assert.IsTrue(ExplorationSessionManager.Instance.CompleteExplorationFailure("이전 저장 호환 검사")); // 이전 저장 복원 후 전멸 처리
            Assert.AreEqual(160, resources.Gold); // 새 획득분만 제거하고 이전 잔액 보존 확인
        } // 재화 통합 검사 종료
        finally // 독립 저장 파일 정리
        { // 저장 정리 시작
            if (File.Exists(path)) // 탐사 저장 파일 확인
            { // 저장 파일 제거 시작
                File.Delete(path); // 독립 저장 파일 삭제
            } // 저장 파일 제거 종료
            if (File.Exists(path + ".bak")) // 탐사 복구본 확인
            { // 복구본 제거 시작
                File.Delete(path + ".bak"); // 독립 복구본 삭제
            } // 복구본 제거 종료
        } // 저장 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 전멸 재화 검사 종료
} // 재화 검사 종료
