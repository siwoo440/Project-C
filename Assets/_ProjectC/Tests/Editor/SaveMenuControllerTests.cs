using System; // 기본 자료형 사용
using System.Collections; // 코루틴 사용
using System.IO; // 독립 저장 경로 사용
using NUnit.Framework; // 테스트 검증 사용
using UnityEngine; // 화면 객체 사용
using UnityEngine.UI; // 버튼 상태 조회 사용
using UnityEngine.TestTools; // 플레이 모드 테스트 사용

public sealed class SaveMenuControllerTests // 저장 화면 연결 검증
{ // 저장 화면 테스트 시작
    [UnityTest] // 플레이 모드 테스트 지정
    public IEnumerator Save_ExistingSlotRequiresConfirmation() // 기존 슬롯 덮어쓰기 확인 검사
    { // 저장 확인 검증 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string directory = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N")); // 독립 테스트 폴더 경로
        Directory.CreateDirectory(directory); // 테스트 폴더 생성
        GameObject canvasObject = new GameObject("SlotSaveTestCanvas", typeof(RectTransform), typeof(Canvas)); // 테스트 캔버스 생성
        try // 테스트 정리 보장
        { // 저장 확인 검사 시작
            SaveSlotService service = new SaveSlotService(directory, Path.Combine(directory, "auto.json"), Path.Combine(directory, "run.json")); // 독립 슬롯 서비스 생성
            PlayerResourceManager resources = PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비
            resources.ResetResources(); // 현재 자원 초기화
            resources.AddResources(11, 0, 0, 0); // 기존 저장 골드 설정
            int originalGold = resources.Gold; // 기존 저장 골드 보관
            Assert.IsTrue(service.SaveLobby(1)); // 기존 슬롯 저장 준비
            resources.AddResources(7, 0, 0, 0); // 변경할 골드 설정
            int changedGold = resources.Gold; // 변경 골드 보관
            SaveMenuController controller = SaveMenuController.Open(canvasObject.GetComponent<Canvas>(), service, true, false, index => true); // 저장 가능한 슬롯 화면 열기
            Transform view = controller.transform.Find("SaveMenu"); // 저장 화면 조회
            view.Find("Slot_1/SaveButton").GetComponent<Button>().onClick.Invoke(); // 기존 슬롯 저장 요청
            Transform confirmation = view.Find("SlotConfirmation"); // 덮어쓰기 확인창 조회
            Assert.IsTrue(confirmation.gameObject.activeSelf); // 덮어쓰기 확인창 표시 확인
            confirmation.Find("CancelButton").GetComponent<Button>().onClick.Invoke(); // 덮어쓰기 취소 실행
            Assert.IsTrue(service.Load(1)); // 기존 저장 불러오기 확인
            Assert.AreEqual(originalGold, resources.Gold); // 취소 후 기존 저장 유지 확인
            resources.AddResources(changedGold - originalGold, 0, 0, 0); // 변경할 골드 재설정
            view.Find("Slot_1/SaveButton").GetComponent<Button>().onClick.Invoke(); // 덮어쓰기 재요청
            confirmation.Find("ConfirmButton").GetComponent<Button>().onClick.Invoke(); // 덮어쓰기 확인 실행
            resources.ResetResources(); // 현재 자원 초기화
            Assert.IsTrue(service.Load(1)); // 변경된 저장 불러오기 확인
            Assert.AreEqual(changedGold, resources.Gold); // 확인 후 변경 저장 복원 확인
        } // 저장 확인 검사 종료
        finally // 테스트 정리 시작
        { // 화면과 파일 정리 시작
            UnityEngine.Object.DestroyImmediate(canvasObject); // 테스트 화면 제거
            Directory.Delete(directory, true); // 독립 테스트 폴더 제거
        } // 화면과 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 저장 확인 검증 종료
    [UnityTest] // 플레이 모드 테스트 지정
    public IEnumerator Delete_RequiresConfirmationAndPreservesOtherSlots() // 삭제 확인과 슬롯 독립성 검사
    { // 삭제 확인 검증 시작
        yield return new EnterPlayMode(); // 플레이 모드 시작
        string directory = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N")); // 독립 테스트 폴더 경로
        Directory.CreateDirectory(directory); // 테스트 폴더 생성
        GameObject canvasObject = new GameObject("SlotTestCanvas", typeof(RectTransform), typeof(Canvas)); // 테스트 캔버스 생성
        try // 테스트 정리 보장
        { // 저장 화면 검사 시작
            SaveSlotService service = new SaveSlotService(directory, Path.Combine(directory, "auto.json"), Path.Combine(directory, "run.json")); // 독립 슬롯 서비스 생성
            Assert.IsTrue(service.SaveLobby(1)); // 첫 슬롯 데이터 준비
            Assert.IsTrue(service.SaveLobby(2)); // 둘째 슬롯 데이터 준비
            Type controllerType = typeof(MainMenuController).Assembly.GetType("SaveMenuController"); // 저장 화면 연결 형식 조회
            Assert.IsNotNull(controllerType); // 저장 화면 연결 존재 확인
            Func<int, bool> onLoad = index => index >= 0; // 불러오기 시험 동작 준비
            Component controller = (Component)controllerType.GetMethod("Open").Invoke(null, new object[] { canvasObject.GetComponent<Canvas>(), service, false, false, onLoad }); // 메인 메뉴 저장 화면 열기
            Transform view = controller.transform.Find("SaveMenu"); // 저장 화면 조회
            Transform firstSlot = view.Find("Slot_1"); // 첫 슬롯 조회
            Assert.IsFalse(firstSlot.Find("SaveButton").GetComponent<Button>().interactable); // 메인 메뉴 저장 차단 확인
            Assert.IsTrue(firstSlot.Find("LoadButton").GetComponent<Button>().interactable); // 저장 파일 불러오기 활성 확인
            firstSlot.Find("DeleteButton").GetComponent<Button>().onClick.Invoke(); // 삭제 요청 실행
            Transform confirmation = view.Find("SlotConfirmation"); // 삭제 확인창 조회
            Assert.IsTrue(confirmation.gameObject.activeSelf); // 삭제 확인창 표시 확인
            Assert.IsNotNull(confirmation.Find("WarningBackdrop").GetComponent<Image>().sprite); // 제작된 확인창 배경 이미지 확인
            Assert.IsNotNull(confirmation.Find("ConfirmButton").GetComponent<Image>().sprite); // 제작된 확인 버튼 이미지 확인
            Assert.IsNotNull(confirmation.Find("CancelButton").GetComponent<Image>().sprite); // 제작된 취소 버튼 이미지 확인
            Assert.IsTrue(service.CanLoad(1)); // 확인 전 파일 유지 확인
            confirmation.Find("CancelButton").GetComponent<Button>().onClick.Invoke(); // 삭제 취소 실행
            Assert.IsTrue(service.CanLoad(1)); // 취소 후 파일 유지 확인
            firstSlot.Find("DeleteButton").GetComponent<Button>().onClick.Invoke(); // 삭제 재요청 실행
            confirmation.Find("ConfirmButton").GetComponent<Button>().onClick.Invoke(); // 삭제 확인 실행
            Assert.IsFalse(service.HasFile(1)); // 선택 슬롯 삭제 확인
            Assert.IsTrue(service.CanLoad(2)); // 다른 슬롯 유지 확인
            Assert.IsFalse(firstSlot.Find("LoadButton").GetComponent<Button>().interactable); // 빈 슬롯 불러오기 차단 확인
        } // 저장 화면 검사 종료
        finally // 테스트 정리 시작
        { // 화면과 파일 정리 시작
            UnityEngine.Object.DestroyImmediate(canvasObject); // 테스트 화면 제거
            Directory.Delete(directory, true); // 독립 테스트 폴더 제거
        } // 화면과 파일 정리 종료
        yield return new ExitPlayMode(); // 플레이 모드 종료
    } // 삭제 확인 검증 종료
} // 저장 화면 테스트 종료
