using System; // 고유 파일 이름 사용
using System.Collections; // 플레이 모드 대기 사용
using System.IO; // 독립 저장 파일 사용
using NUnit.Framework; // 저장 복구 검증 사용
using UnityEngine; // 게임 자원과 임시 경로 사용
using UnityEngine.TestTools; // 플레이 모드 테스트 사용
using UnityEngine.UI; // 저장 화면 버튼 검사 사용
using TMPro; // 저장 화면 안내 검사 사용

public sealed class SaveRecoveryTests // 저장 손상과 복구 검증
{ // 저장 복구 테스트 시작
    [UnityTest] // 플레이 모드 테스트 지정
    public IEnumerator LobbySave_PreservesPreviousValidBackupAndProtectsFutureVersions() // 거점 저장 복구와 버전 보호 확인
    { // 거점 저장 복구 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string root = Path.Combine(Application.temporaryCachePath, "save-recovery-" + Guid.NewGuid().ToString("N")); // 독립 테스트 폴더 경로 생성
        string main = Path.Combine(root, "slot-1.json"); // 독립 수동 저장 경로 생성
        try // 테스트 파일 정리 보장
        { // 거점 저장 복구 검증 시작
            PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 실제 자원 관리자 준비
            resources.ResetResources(); // 테스트 자원 초기화
            resources.AddResources(10, 0, 0, 0); // 첫 정상 골드 설정
            Assert.IsTrue(LobbySaveService.Save(main)); // 첫 정상 거점 저장 확인
            resources.AddResources(10, 0, 0, 0); // 두 번째 골드 설정
            Assert.IsTrue(LobbySaveService.Save(main)); // 두 번째 거점 저장 확인
            Assert.IsTrue(File.Exists(main + ".bak")); // 이전 정상 저장 복구본 존재 확인
            File.WriteAllText(main, "손상된 저장"); // 현재 저장 파일 손상 재현
            SaveSlotService slots = new SaveSlotService(root, Path.Combine(root, "auto-lobby"), Path.Combine(root, "auto-run")); // 독립 슬롯 서비스 생성
            Assert.IsTrue(slots.CanLoad(1)); // 이전 정상 복구본 불러오기 확인
            Assert.IsTrue(slots.IsRecovery(1)); // 복구본 사용 상태 표시 확인
            GameObject canvasObject = new GameObject("RecoveryCanvas", typeof(RectTransform), typeof(Canvas)); // 독립 저장 화면 캔버스 생성
            bool requested = false; // 불러오기 확인 여부 보관
            SaveMenuController menu = SaveMenuController.Open(canvasObject.GetComponent<Canvas>(), slots, true, false, index => // 실제 저장 화면 표시
            { // 불러오기 동작 시작
                requested = true; // 불러오기 요청 기록
                return true; // 불러오기 성공 반환
            }); // 저장 화면 생성 종료
            Transform row = menu.transform.Find("SaveMenu/Slot_1"); // 복구 슬롯 화면 조회
            StringAssert.Contains("이전 저장 복구", row.Find("Description").GetComponent<TMP_Text>().text); // 복구 슬롯 안내 확인
            row.Find("LoadButton").GetComponent<Button>().onClick.Invoke(); // 복구 슬롯 불러오기 요청
            Transform confirmation = menu.transform.Find("SaveMenu/SlotConfirmation"); // 복구 확인 화면 조회
            Assert.IsTrue(confirmation.gameObject.activeSelf); // 복구 확인창 표시 확인
            Assert.IsFalse(requested); // 확인 전 불러오기 차단 확인
            confirmation.Find("CancelButton").GetComponent<Button>().onClick.Invoke(); // 복구 요청 취소
            Assert.IsFalse(requested); // 취소 시 불러오기 차단 확인
            row.Find("LoadButton").GetComponent<Button>().onClick.Invoke(); // 복구 슬롯 재요청
            confirmation.Find("ConfirmButton").GetComponent<Button>().onClick.Invoke(); // 복구 불러오기 확인
            Assert.IsTrue(requested); // 확인 후 불러오기 실행 확인
            UnityEngine.Object.DestroyImmediate(canvasObject); // 독립 저장 화면 제거
            resources.ResetResources(); // 현재 자원 제거
            Assert.IsTrue(slots.Load(1)); // 이전 정상 거점 복원 확인
            Assert.AreEqual(10, resources.Gold); // 마지막 정상 복구본의 자원 확인
            Assert.IsTrue(LobbySaveService.Save(main)); // 손상 원본 위 새 정상 저장 확인
            Assert.IsTrue(File.Exists(main + ".bak")); // 손상 원본 교체 시 복구본 보존 확인
            File.Delete(main); // 원본 파일 누락 상황 재현
            Assert.IsTrue(slots.CanLoad(1)); // 원본 누락 시 정상 복구본 사용 확인
            Assert.IsTrue(slots.IsRecovery(1)); // 원본 누락의 복구 상태 확인
            resources.AddResources(20, 0, 0, 0); // 새 정상 저장 자원 설정
            Assert.IsTrue(LobbySaveService.Save(main)); // 원본 누락 상태에서 새 저장 확인
            Assert.IsTrue(File.Exists(main + ".bak")); // 새 저장 시 이전 복구본 보존 확인
            File.WriteAllText(main + ".bak", "{\"version\":2}"); // 지원하지 않는 새 복구본 버전 재현
            Assert.IsFalse(LobbySaveService.Save(main)); // 새 버전 복구본 덮어쓰기 차단 확인
            Assert.AreEqual("{\"version\":2}", File.ReadAllText(main + ".bak")); // 새 버전 복구본 보존 확인
            string incomplete = Path.Combine(root, "slot-2.json.orphan.tmp"); // 중단된 임시 저장 경로 생성
            File.WriteAllText(incomplete, "미완료"); // 중단된 임시 파일 재현
            Assert.IsFalse(slots.HasFile(2)); // 미완료 임시 파일을 저장 슬롯으로 오인하지 않음 확인
            Assert.IsFalse(slots.CanLoad(2)); // 미완료 임시 파일 복원 차단 확인
            File.Delete(incomplete); // 독립 임시 파일 제거
            File.WriteAllText(main, "{\"version\":2}"); // 지원하지 않는 새 저장 버전 재현
            string future = File.ReadAllText(main); // 새 버전 원본 내용 보관
            Assert.IsFalse(slots.CanLoad(1)); // 새 버전에서 오래된 복구본 자동 사용 차단 확인
            Assert.IsTrue(slots.IsUnsupportedVersion(1)); // 지원하지 않는 버전 표시 확인
            Assert.IsFalse(LobbySaveService.Save(main)); // 새 버전 원본 덮어쓰기 차단 확인
            Assert.AreEqual(future, File.ReadAllText(main)); // 새 버전 원본 보존 확인
            Assert.IsTrue(File.Exists(main + ".bak")); // 이전 정상 복구본 보존 확인
            Assert.IsTrue(slots.Delete(1)); // 사용자 요청 슬롯 삭제 확인
            Assert.IsFalse(File.Exists(main)); // 원본 저장 삭제 확인
            Assert.IsFalse(File.Exists(main + ".bak")); // 삭제된 슬롯 복구본 제거 확인
        } // 거점 저장 복구 검증 종료
        finally // 독립 테스트 파일 정리
        { // 테스트 파일 정리 시작
            if (File.Exists(main + ".bak")) // 독립 복구본 파일 존재 확인
            { // 독립 복구본 제거 시작
                File.Delete(main + ".bak"); // 독립 복구본 파일 삭제
            } // 독립 복구본 제거 종료
            if (File.Exists(main)) // 독립 원본 파일 존재 확인
            { // 독립 원본 제거 시작
                File.Delete(main); // 독립 원본 파일 삭제
            } // 독립 원본 제거 종료
            if (Directory.Exists(root)) // 독립 테스트 폴더 존재 확인
            { // 테스트 폴더 제거 시작
                Directory.Delete(root); // 빈 독립 테스트 폴더 삭제
            } // 테스트 폴더 제거 종료
        } // 테스트 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 거점 저장 복구 검사 종료
    [UnityTest] // 탐사 저장 복구 테스트 지정
    public IEnumerator MixedSaveSlot_PrefersCurrentTypeAndRecoversPreviousExploration() // 저장 종류 전환과 탐사 복구 확인
    { // 혼합 저장 복구 검사 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string root = Path.Combine(Application.temporaryCachePath, "mixed-recovery-" + Guid.NewGuid().ToString("N")); // 독립 테스트 폴더 경로 생성
        string path = Path.Combine(root, "slot-1.json"); // 독립 수동 저장 경로 생성
        try // 테스트 파일 정리 보장
        { // 혼합 저장 복구 검증 시작
            CharacterData character = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster").Characters[0]; // 실제 출전 캐릭터 조회
            PartySelection selection = new PartySelection(new CharacterRecoveryDeploymentStatusProvider(CharacterRecoveryManager.EnsureInstance())); // 출전 파티 선택 생성
            Assert.IsTrue(selection.TryAdd(character)); // 실제 캐릭터 선택 확인
            Assert.IsTrue(PartyLoadoutRuntime.Prepare(selection)); // 출전 파티와 덱 준비
            RunDeckManager.EnsureInstance().ResetToSource(PartyLoadoutRuntime.Active.Deck); // 탐사 덱 초기화
            ExplorationSessionManager session = ExplorationSessionManager.EnsureInstance(); // 탐사 상태 관리자 준비
            session.ResetExploration(); // 독립 탐사 상태 준비
            session.SetCurrentFloorSeed(456789); // 고정 탐사 맵 시드 설정
            Vector3 first = new Vector3(1f, 2f, 0f); // 첫 탐사 저장 위치
            Vector3 second = new Vector3(4f, 5f, 0f); // 두 번째 탐사 저장 위치
            Assert.IsTrue(ExplorationSaveService.Save(path, first)); // 첫 탐사 저장 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, second)); // 두 번째 탐사 저장 확인
            File.WriteAllText(path, "손상된 탐사 저장"); // 현재 탐사 파일 손상 재현
            Assert.IsTrue(ExplorationSaveService.Load(path)); // 이전 정상 탐사 저장 복원 확인
            Assert.AreEqual(first, session.GetPlayerSpawnPosition(Vector3.zero)); // 이전 탐사 위치 복원 확인
            Assert.IsTrue(ExplorationSaveService.Save(path, second)); // 손상 원본 위 새 정상 저장 확인
            Assert.IsTrue(LobbySaveService.Save(path)); // 같은 슬롯에 거점 저장으로 전환
            SaveSlotService slots = new SaveSlotService(root, Path.Combine(root, "auto-lobby"), Path.Combine(root, "auto-run")); // 독립 슬롯 서비스 생성
            Assert.IsFalse(slots.IsExploration(1)); // 정상 거점 원본보다 오래된 탐사 복구본 우선 차단
            File.WriteAllText(path, "손상된 거점 저장"); // 현재 거점 파일 손상 재현
            Assert.IsTrue(slots.IsRecovery(1)); // 이전 탐사 복구본 표시 확인
            Assert.IsTrue(slots.IsExploration(1)); // 이전 탐사 복구본 종류 확인
            Assert.IsTrue(slots.Load(1)); // 이전 탐사 복구본 불러오기 확인
            Assert.AreEqual(second, session.GetPlayerSpawnPosition(Vector3.zero)); // 이전 탐사 위치 재확인
            Assert.IsTrue(ExplorationSaveService.Remove(path)); // 탐사 저장과 복구본 삭제 확인
            Assert.IsFalse(File.Exists(path)); // 탐사 원본 삭제 확인
            Assert.IsFalse(File.Exists(path + ".bak")); // 탐사 복구본 삭제 확인
        } // 혼합 저장 복구 검증 종료
        finally // 독립 테스트 파일 정리
        { // 테스트 파일 정리 시작
            if (File.Exists(path + ".bak")) // 독립 복구본 파일 존재 확인
            { // 복구본 정리 시작
                File.Delete(path + ".bak"); // 독립 복구본 삭제
            } // 복구본 정리 종료
            if (File.Exists(path)) // 독립 원본 파일 존재 확인
            { // 원본 정리 시작
                File.Delete(path); // 독립 원본 삭제
            } // 원본 정리 종료
            if (Directory.Exists(root)) // 독립 폴더 존재 확인
            { // 폴더 정리 시작
                Directory.Delete(root); // 빈 독립 폴더 삭제
            } // 폴더 정리 종료
        } // 테스트 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 혼합 저장 복구 검사 종료
} // 저장 복구 테스트 종료
