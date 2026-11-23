using UnityEngine; // 카드 이동 좌표와 시간 사용
using UnityEngine.UI; // 카드 이동 이미지 사용
public sealed class BattleCardFlightView : MonoBehaviour // 카드 더미 이동 연출 화면
{ // 카드 이동 화면 시작
    private RectTransform cardRect; // 이동 카드 사각형
    private float elapsed; // 이동 진행 시간
    private float duration; // 이동 연출 시간
    private float startScale; // 시작 카드 배율
    private float endScale; // 도착 카드 배율
    public bool IsDrawing { get; private set; } // 카드 뽑기 방향 여부
    public Vector2 StartPosition { get; private set; } // 손패 기준 출발 위치
    public Vector2 EndPosition { get; private set; } // 손패 기준 도착 위치
    public static void Create(RectTransform parent, CardInstance card, Vector2 start, Vector2 end, bool drawing, float cardScale) // 실제 카드 이동 화면 생성
    { // 카드 이동 화면 생성 시작
        GameObject flightObject = new GameObject("CardFlight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleCardFlightView)); // 이동 카드 화면 생성
        flightObject.transform.SetParent(parent, false); // 전투 손패 위에 이동 화면 연결
        BattleCardFlightView flight = flightObject.GetComponent<BattleCardFlightView>(); // 이동 화면 컴포넌트 조회
        flight.cardRect = flightObject.GetComponent<RectTransform>(); // 이동 카드 사각형 조회
        flight.cardRect.anchorMin = new Vector2(0.5f, 0.5f); // 이동 카드 최소 중앙 앵커
        flight.cardRect.anchorMax = new Vector2(0.5f, 0.5f); // 이동 카드 최대 중앙 앵커
        flight.cardRect.pivot = new Vector2(0.5f, 0.5f); // 이동 카드 중앙 기준점
        flight.cardRect.sizeDelta = new Vector2(BattleCardView.CardWidth, BattleCardView.CardHeight); // 실제 카드 기본 크기 적용
        flight.StartPosition = start; // 실제 출발 위치 저장
        flight.EndPosition = end; // 실제 도착 위치 저장
        flight.IsDrawing = drawing; // 이동 방향 저장
        flight.duration = 0.4f; // 이동 연출 시간 설정
        flight.startScale = drawing ? 0.32f : cardScale; // 출발 위치의 카드 배율 설정
        flight.endScale = drawing ? cardScale : 0.22f; // 도착 위치의 카드 배율 설정
        flight.cardRect.anchoredPosition = start; // 출발 지점에 카드 배치
        flight.cardRect.localScale = Vector3.one * flight.startScale; // 출발 카드 배율 적용
        Image frame = flightObject.GetComponent<Image>(); // 카드 테두리 이미지 조회
        BattleUiArt.Apply(frame, "CardFrame"); // 실제 카드 테두리 적용
        frame.raycastTarget = false; // 이동 카드의 입력 차단 방지
        GameObject artObject = new GameObject("Artwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 이동 카드 일러스트 생성
        artObject.transform.SetParent(flightObject.transform, false); // 일러스트를 이동 카드에 연결
        RectTransform artRect = artObject.GetComponent<RectTransform>(); // 일러스트 사각형 조회
        artRect.anchorMin = new Vector2(0.5f, 1f); // 일러스트 위쪽 중앙 최소 앵커
        artRect.anchorMax = new Vector2(0.5f, 1f); // 일러스트 위쪽 중앙 최대 앵커
        artRect.pivot = new Vector2(0.5f, 0.5f); // 일러스트 중앙 기준점
        artRect.anchoredPosition = new Vector2(0f, -51f); // 실제 카드 일러스트 위치 적용
        artRect.sizeDelta = new Vector2(132f, 82f); // 실제 카드 일러스트 크기 적용
        Image art = artObject.GetComponent<Image>(); // 일러스트 이미지 조회
        art.sprite = card.Artwork; // 이동한 카드의 실제 일러스트 적용
        art.color = card.Artwork != null ? Color.white : new Color(0.22f, 0.22f, 0.26f, 1f); // 이미지 누락 시 기본 색상 적용
        art.raycastTarget = false; // 일러스트의 입력 차단 방지
    } // 카드 이동 화면 생성 종료
    private void Update() // 실제 시간 기준 카드 이동
    { // 카드 이동 시작
        elapsed += Time.unscaledDeltaTime; // 전투 속도와 무관한 경과 시간 누적
        float progress = Mathf.Clamp01(elapsed / duration); // 카드 이동 진행률 계산
        float smooth = progress * progress * (3f - 2f * progress); // 부드러운 시작과 정지 계산
        cardRect.anchoredPosition = Vector2.Lerp(StartPosition, EndPosition, smooth); // 실제 카드 출발점과 도착점 사이 이동
        cardRect.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, smooth); // 카드 이동에 따른 크기 변경
        if (progress >= 1f) // 카드 이동 완료 확인
        { // 이동 완료 처리 시작
            Destroy(gameObject); // 완료된 이동 화면 제거
        } // 이동 완료 처리 종료
    } // 카드 이동 종료
} // 카드 이동 화면 종료
