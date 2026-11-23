using System; // 버튼 동작 위임 사용
using NUnit.Framework; // 테스트 검증 기능 사용
using TMPro; // 슬롯 글자 조회 사용
using UnityEngine; // 유니티 화면 객체 사용
using UnityEngine.UI; // 버튼과 이미지 조회 사용

public sealed class SaveMenuViewTests // 저장 화면 구성 검증
{ // 저장 화면 테스트 시작
    [Test] // 편집기 테스트 지정
    public void SaveMenu_CreatesFiveSlotsAndIndependentButtons() // 슬롯과 버튼 구성 확인
    { // 화면 구성 검증 시작
        Type viewType = typeof(MainMenuController).Assembly.GetType("SaveMenuView"); // 저장 화면 형식 조회
        Assert.IsNotNull(viewType); // 저장 화면 기능 존재 확인
        GameObject canvasObject = new GameObject("SaveMenuTestCanvas", typeof(RectTransform), typeof(Canvas)); // 테스트 캔버스 생성
        try // 테스트 화면 정리 보장
        { // 저장 화면 검사 시작
            int saved = -1; // 저장 버튼 결과 초기화
            int loaded = -1; // 불러오기 버튼 결과 초기화
            int deleted = -1; // 삭제 버튼 결과 초기화
            bool closed = false; // 닫기 버튼 결과 초기화
            Action<int> onSave = index => saved = index; // 저장 버튼 시험 동작
            Action<int> onLoad = index => loaded = index; // 불러오기 버튼 시험 동작
            Action<int> onDelete = index => deleted = index; // 삭제 버튼 시험 동작
            Action onClose = () => closed = true; // 닫기 버튼 시험 동작
            Component view = (Component)viewType.GetMethod("Create").Invoke(null, new object[] // 저장 화면 생성 요청
            { // 화면 생성 인수 시작
                canvasObject.GetComponent<Canvas>(), // 테스트 캔버스 전달
                onSave, // 저장 동작 전달
                onLoad, // 불러오기 동작 전달
                onDelete, // 삭제 동작 전달
                onClose // 닫기 동작 전달
            }); // 화면 생성 인수 종료
            Assert.IsNotNull(view); // 저장 화면 생성 확인
            Image background = view.transform.Find("Background").GetComponent<Image>(); // 저장 화면 배경 조회
            Assert.IsNotNull(background.sprite); // 저장 화면 배경 이미지 연결 확인
            Assert.AreSame(Resources.Load<Texture2D>("UI/SaveMenu/Background"), background.sprite.texture); // 제작 배경 이미지 사용 확인
            for (int index = 0; index < 5; index++) // 자동 저장과 네 슬롯 순회
            { // 슬롯 구성 검사 시작
                Transform slot = view.transform.Find("Slot_" + index); // 개별 저장 슬롯 조회
                Assert.IsNotNull(slot, "슬롯 " + index); // 슬롯 존재 확인
                Assert.AreEqual(index == 0 ? "자동 저장" : "슬롯 " + index, slot.Find("SlotName").GetComponent<TMP_Text>().text); // 슬롯 이름 확인
                Assert.AreEqual("데이터 없음", slot.Find("Description").GetComponent<TMP_Text>().text); // 빈 슬롯 안내 확인
                Assert.IsNotNull(slot.Find("SaveButton").GetComponent<Image>().sprite); // 저장 버튼 이미지 확인
                Assert.IsNotNull(slot.Find("LoadButton").GetComponent<Image>().sprite); // 불러오기 버튼 이미지 확인
                Assert.IsNotNull(slot.Find("DeleteButton").GetComponent<Image>().sprite); // 삭제 버튼 이미지 확인
            } // 슬롯 구성 검사 종료
            viewType.GetMethod("SetSlotState").Invoke(view, new object[] // 저장된 슬롯 표시 요청
            { // 슬롯 표시 인수 시작
                4, // 슬롯 번호 전달
                "세룰리온 거점", // 저장 위치 전달
                "2026-11-15 12:00", // 저장 시각 전달
                true // 저장 데이터 존재 전달
            }); // 슬롯 표시 인수 종료
            Transform selected = view.transform.Find("Slot_4"); // 네 번째 수동 슬롯 조회
            selected.Find("SaveButton").GetComponent<Button>().onClick.Invoke(); // 저장 버튼 실행
            selected.Find("LoadButton").GetComponent<Button>().onClick.Invoke(); // 불러오기 버튼 실행
            selected.Find("DeleteButton").GetComponent<Button>().onClick.Invoke(); // 삭제 버튼 실행
            Assert.AreEqual(4, saved); // 저장 버튼 슬롯 확인
            Assert.AreEqual(4, loaded); // 불러오기 버튼 슬롯 확인
            Assert.AreEqual(4, deleted); // 삭제 버튼 슬롯 확인
            Assert.AreEqual("세룰리온 거점", selected.Find("Description").GetComponent<TMP_Text>().text); // 저장 위치 표시 확인
            view.transform.Find("CloseButton").GetComponent<Button>().onClick.Invoke(); // 닫기 버튼 실행
            Assert.IsTrue(closed); // 닫기 동작 확인
        } // 저장 화면 검사 종료
        finally // 테스트 화면 정리
        { // 화면 정리 시작
            UnityEngine.Object.DestroyImmediate(canvasObject); // 테스트 캔버스 제거
        } // 화면 정리 종료
    } // 화면 구성 검증 종료
} // 저장 화면 테스트 종료
