using System.Collections.Generic; // 독립 안내 저장소 사용
using NUnit.Framework; // 안내 화면 검사 사용
using TMPro; // 안내 문구 검사 사용
using UnityEngine; // 화면 위치와 객체 사용
using UnityEngine.UI; // 실제 안내 버튼 사용
public sealed class WorldTutorialViewTests // 거점과 탐사 안내 화면 검사
{ // 안내 화면 검사 시작
    [Test] // 탐사 화면 안내 입력 검사 지정
    public void ExplorationView_AllowsMovementAndReplaysAfterSkip() // 탐사 안내의 입력 통과와 다시 보기 확인
    { // 탐사 화면 검사 시작
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)); // 독립 탐사 캔버스 생성
        Vector2 position = Vector2.zero; // 가상 플레이어 시작 위치 생성
        WorldTutorialProgress progress = new WorldTutorialProgress(WorldTutorialKind.Exploration, new MemoryStore()); // 독립 탐사 안내 생성
        WorldTutorialView view = WorldTutorialView.Create(canvasObject.GetComponent<Canvas>(), progress, () => position, step => null, true); // 실제 탐사 안내 화면 생성
        progress.StartIfNeeded(true, position); // 탐사 안내 시작
        Transform guide = view.transform.Find("Guide"); // 안내 화면 조회
        Assert.IsFalse(guide.GetComponent<Image>().raycastTarget); // 탐사 안내 배경의 이동 입력 차단 해제 확인
        var background = guide.Find("Panel").GetComponent<Image>(); // 탐사 창의 공통 배경 조회
        Assert.IsNotNull(background.sprite); // 탐사에 적용한 제작 배경 확인
        Assert.AreEqual("TutorialPanel", background.sprite.name); // 전투와 같은 제작 원본 확인
        Assert.AreEqual(Image.Type.Sliced, background.type); // 실제 탐사 창의 금속 모서리 유지 확인
        TMP_Text page = guide.Find("Panel/PageLabel").GetComponent<TMP_Text>(); // 안내 페이지 표시 조회
        Button previous = guide.Find("Panel/PreviousPage").GetComponent<Button>(); // 이전 페이지 버튼 조회
        Button forward = guide.Find("Panel/NextButton").GetComponent<Button>(); // 유일한 다음 페이지 버튼 조회
        Button close = guide.Find("Panel/SkipButton").GetComponent<Button>(); // 위쪽 즉시 닫기 버튼 조회
        Assert.IsNull(guide.Find("Panel/NextPage")); // 중복 페이지 이동 버튼 제거 확인
        Assert.AreEqual(3, guide.Find("Panel").GetComponentsInChildren<Button>().Length); // 이전·다음·닫기 버튼 수 확인
        Assert.AreEqual("이전", previous.GetComponentInChildren<TMP_Text>().text); // 이전 이동 버튼 문구 확인
        Assert.AreEqual("다음", forward.GetComponentInChildren<TMP_Text>().text); // 다음 이동 버튼 문구 확인
        Assert.AreEqual("X", close.GetComponentInChildren<TMP_Text>().text); // 즉시 닫기 버튼 문구 확인
        Assert.AreEqual("페이지 1 / 5", page.text); // 탐사 안내 첫 페이지 표시 확인
        Assert.IsFalse(previous.interactable); // 첫 페이지의 이전 이동 차단 확인
        Assert.IsFalse(forward.interactable); // 실제 이동 전 다음 페이지 차단 확인
        Assert.AreSame(Resources.Load<Texture2D>("UI/Tutorial/PageButton"), previous.image.sprite.texture); // 제작한 이전 페이지 버튼 이미지 확인
        Assert.AreSame(previous.image.sprite, forward.image.sprite); // 이전과 다음 버튼 이미지 공유 확인
        Assert.Greater(previous.GetComponent<RectTransform>().sizeDelta.x, previous.GetComponent<RectTransform>().sizeDelta.y); // 가로로 긴 페이지 버튼 크기 확인
        RawImage preview = guide.Find("Panel/Preview/ExampleImage").GetComponent<RawImage>(); // 실제 탐사 예시 이미지 조회
        Assert.AreSame(Resources.Load<Texture2D>("UI/Tutorial/Exploration"), preview.texture); // 왼쪽 실제 탐사 예시 이미지 확인
        Assert.Less(preview.rectTransform.parent.GetComponent<RectTransform>().anchorMax.x, guide.Find("Panel/Title").GetComponent<RectTransform>().anchorMin.x); // 예시 이미지와 오른쪽 설명 배치 확인
        Assert.IsTrue(guide.Find("Panel/Description").GetComponent<TMP_Text>().text.Contains("WASD")); // 실제 이동 키 안내 확인
        position = new Vector2(0.2f, 0f); // 실제 이동한 위치 준비
        progress.ObservePosition(position); // 실제 이동 위치 전달
        Assert.IsTrue(forward.interactable); // 이동 후 다음 페이지 버튼 활성 확인
        forward.onClick.Invoke(); // 오른쪽 버튼으로 다음 안내 선택
        Assert.AreEqual("페이지 2 / 5", page.text); // 다음 페이지 번호 표시 확인
        previous.onClick.Invoke(); // 왼쪽 버튼으로 이전 안내 선택
        Assert.AreEqual("페이지 1 / 5", page.text); // 이전 페이지 번호 표시 확인
        Assert.IsTrue(forward.interactable); // 돌아와도 실제 이동 기록 유지 확인
        forward.onClick.Invoke(); // 탐사 상호작용 안내 이동
        StringAssert.Contains("F 키", guide.Find("Panel/Description").GetComponent<TMP_Text>().text); // 현재 탐사 상호작용 키 안내 확인
        string[] images = { "Event", "Treasure", "Rest", "Shop" }; // 각 방의 실제 촬영 이미지 이름
        string[] rooms = { "이벤트", "보물", "휴식", "상점" }; // 방별 안내 제목
        for (int index = 0; index < images.Length; index++) // 이동 다음의 네 페이지 검사
        { // 방별 안내 확인 시작
            Texture2D expected = Resources.Load<Texture2D>("UI/Tutorial/" + images[index]); // 실제 방 이미지 조회
            Assert.IsNotNull(expected, rooms[index]); // 이미지가 빠진 방 확인
            Assert.AreSame(expected, preview.texture); // 현재 방의 예시 화면 표시 확인
            Assert.AreEqual(rooms[index], guide.Find("Panel/Title").GetComponent<TMP_Text>().text); // 현재 방 이름 확인
            if (index + 1 < images.Length) // 다음 방 안내 존재 확인
            { // 다음 페이지 이동 시작
                forward.onClick.Invoke(); // 오른쪽 화살표로 다음 방 열기
            } // 다음 페이지 이동 종료
        } // 방별 안내 확인 종료
        Assert.AreEqual("페이지 5 / 5", page.text); // 표시한 마지막 탐사 페이지 확인
        Assert.AreEqual("완료", forward.GetComponentInChildren<TMP_Text>().text); // 마지막 장의 완료 문구 확인
        previous.onClick.Invoke(); // 마지막 안내의 이전 페이지 이동
        Assert.AreEqual("다음", forward.GetComponentInChildren<TMP_Text>().text); // 마지막 페이지를 벗어난 다음 문구 복구 확인
        forward.onClick.Invoke(); // 다시 마지막 페이지 이동
        forward.onClick.Invoke(); // 마지막 장에서 안내 완료
        Assert.IsFalse(guide.gameObject.activeSelf); // 완료한 안내 즉시 종료 확인
        view.transform.Find("ReplayButton").GetComponent<Button>().onClick.Invoke(); // 탐사 안내 다시 보기 클릭
        Assert.IsTrue(guide.gameObject.activeSelf); // 안내 다시 보기 표시 확인
        Assert.IsFalse(forward.interactable); // 다시 보기 후 새로운 이동 대기 확인
        close.onClick.Invoke(); // 이동 없이 첫 페이지에서 안내 닫기
        Assert.IsFalse(guide.gameObject.activeSelf); // 첫 장에서도 즉시 닫히는지 확인
        Object.DestroyImmediate(canvasObject); // 임시 화면 정리
    } // 탐사 화면 검사 종료
    [Test] // 거점 튜토리얼 실제 순서 검사
    public void LobbyView_DisplaysEachListedPageUntilFinish() // 거점 전체 페이지와 마지막 버튼 확인
    { // 거점 페이지 검사 시작
        var root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)); // 독립 거점 화면 준비
        var progress = new WorldTutorialProgress(WorldTutorialKind.Lobby, new MemoryStore()); // 실제 거점 진행 상태
        var view = WorldTutorialView.Create(root.GetComponent<Canvas>(), progress, () => Vector2.zero, _ => null, false); // 실제 거점 안내 레이아웃
        progress.StartIfNeeded(true, Vector2.zero); // 첫 안내 페이지 열기
        var guide = view.transform.Find("Guide"); // 표시되는 거점 안내 화면
        var title = guide.Find("Panel/Title").GetComponent<TMP_Text>(); // 현재 페이지 제목 조회
        var page = guide.Find("Panel/PageLabel").GetComponent<TMP_Text>(); // 목록과 함께 표시한 장수 조회
        var next = guide.Find("Panel/NextButton").GetComponent<Button>(); // 공용 이동 버튼 조회
        var label = next.GetComponentInChildren<TMP_Text>(); // 실제 버튼 문구 조회
        string[] titles = { "거점과 캐릭터", "탐사 파티 편성", "치료와 부활 대기", "설비 강화", "다음 탐사 준비" }; // 실제 제목 순서
        for (int index = 0; index < titles.Length; index++) // 화면의 모든 거점 페이지 확인
        { // 현재 장의 제목과 진행 확인 시작
            Assert.AreEqual(titles[index], title.text); // 빠짐없는 실제 페이지 확인
            Assert.AreEqual($"페이지 {index + 1} / {titles.Length}", page.text); // 실제 목록 기반 전체 장수 확인
            Assert.AreEqual(index == titles.Length - 1 ? "완료" : "다음", label.text); // 현재 화면의 이동 문구 확인
            next.onClick.Invoke(); // 정상 다음 버튼으로 진행
        } // 거점 페이지 확인 종료
        Assert.IsFalse(guide.gameObject.activeSelf); // 마지막 장의 완료 버튼으로 닫기 확인
        Object.DestroyImmediate(root); // 검사 화면 객체 제거
    } // 거점 페이지 검사 종료
    private sealed class MemoryStore : ISettingsStore // 안내 화면 검사용 저장소
    { // 임시 저장소 시작
        private readonly Dictionary<string, int> values = new Dictionary<string, int>(); // 설정 값 목록
        public int Read(string key, int fallback) // 임시 설정 값 조회
        { // 설정 조회 시작
            return values.TryGetValue(key, out int value) ? value : fallback; // 저장 값 또는 기본값 반환
        } // 설정 조회 종료
        public void Write(string key, int value) // 임시 설정 값 기록
        { // 설정 기록 시작
            values[key] = value; // 설정 값 저장
        } // 설정 기록 종료
    } // 임시 저장소 종료
} // 안내 화면 검사 종료
