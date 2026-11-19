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
        TMP_Text page = guide.Find("Panel/PageLabel").GetComponent<TMP_Text>(); // 안내 페이지 표시 조회
        Button previous = guide.Find("Panel/PreviousPage").GetComponent<Button>(); // 이전 페이지 버튼 조회
        Button forward = guide.Find("Panel/NextPage").GetComponent<Button>(); // 다음 페이지 버튼 조회
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
        Button next = guide.Find("Panel/NextButton").GetComponent<Button>(); // 다음 안내 버튼 조회
        Assert.IsFalse(next.interactable); // 이동 전 다음 버튼 비활성 확인
        position = new Vector2(0.2f, 0f); // 실제 이동한 위치 준비
        progress.ObservePosition(position); // 실제 이동 위치 전달
        Assert.IsTrue(next.interactable); // 이동 후 다음 버튼 활성 확인
        Assert.IsTrue(forward.interactable); // 이동 후 다음 페이지 버튼 활성 확인
        forward.onClick.Invoke(); // 오른쪽 버튼으로 다음 안내 선택
        Assert.AreEqual("페이지 2 / 5", page.text); // 다음 페이지 번호 표시 확인
        previous.onClick.Invoke(); // 왼쪽 버튼으로 이전 안내 선택
        Assert.AreEqual("페이지 1 / 5", page.text); // 이전 페이지 번호 표시 확인
        Assert.IsTrue(forward.interactable); // 돌아와도 실제 이동 기록 유지 확인
        next.onClick.Invoke(); // 접촉 상호작용 안내 이동
        Assert.IsTrue(guide.Find("Panel/Description").GetComponent<TMP_Text>().text.Contains("접촉")); // 실제 접촉 상호작용 안내 확인
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
        guide.Find("Panel/SkipButton").GetComponent<Button>().onClick.Invoke(); // 탐사 안내 건너뛰기
        Assert.IsFalse(guide.gameObject.activeSelf); // 건너뛴 안내 숨김 확인
        view.transform.Find("ReplayButton").GetComponent<Button>().onClick.Invoke(); // 탐사 안내 다시 보기 클릭
        Assert.IsTrue(guide.gameObject.activeSelf); // 안내 다시 보기 표시 확인
        Assert.IsFalse(next.interactable); // 다시 보기 후 새로운 이동 대기 확인
        Object.DestroyImmediate(canvasObject); // 임시 화면 정리
    } // 탐사 화면 검사 종료
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
