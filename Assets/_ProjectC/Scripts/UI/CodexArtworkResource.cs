using UnityEngine; // 원화 스프라이트 참조 사용

public sealed class CodexArtworkResource : ScriptableObject // 상세 화면 한 곳에서 읽는 단일 원화 참조
{ // 상세 원화 리소스 시작
    [SerializeField] private Sprite artwork; // 한 항목의 기존 게임 원화
    public Sprite Artwork => artwork; // 상세 창의 실제 원화 반환
} // 상세 원화 리소스 종료
