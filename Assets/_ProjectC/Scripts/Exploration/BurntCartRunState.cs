using System; // 저장용 직렬화와 배열 검색
using UnityEngine; // 저장 필드와 가격 계산

[Serializable] // 탐사 저장 파일의 수레 상태
public sealed class BurntCartRunState // 수레 매복과 다음 상점 할인 보관
{ // 수레 상태 시작
    [SerializeField] private string ambushRoom; // 매복을 일으킨 이벤트 방
    [SerializeField] private string normalEncounter; // 매복에 사용한 일반 조우 원본
    [SerializeField] private string markRoom; // 표식을 남긴 이벤트 방
    [SerializeField] private string markedShop; // 표식 이후 실제 처음 연 상점
    public string AmbushRoom => string.IsNullOrEmpty(ambushRoom) ? null : ambushRoom; // 저장의 빈 문자열과 초기 상태를 같은 미예약 값으로 처리
    public string NormalEncounter => normalEncounter; // 복원할 일반 적 편성 조회
    public bool HasMark => !string.IsNullOrEmpty(markRoom); // 미사용 상점 표식 확인
    public string AmbushBattleId => string.IsNullOrEmpty(ambushRoom) ? null : "cart_ambush_" + ambushRoom; // 일반 방과 구분한 매복 전투 식별자

    internal void SetAmbush(string room, string encounter) // 매복 전투 직전 상태 보관
    { // 매복 기록 시작
        ambushRoom = room; // 매복을 발생시킨 방 저장
        normalEncounter = encounter; // 추첨한 일반 적 편성 저장
    } // 매복 기록 종료

    internal void FinishAmbush(string battleId) // 승리와 도주 및 패배 후 매복 해제
    { // 매복 해제 시작
        if (AmbushRoom != null && battleId == AmbushBattleId) // 이번 전투와 대기 매복 일치 확인
        { // 해당 매복 해제 시작
            ambushRoom = null; // 이벤트 방 매복 해제
            normalEncounter = null; // 원래 편성 참조 해제
        } // 해당 매복 해제 종료
    } // 매복 해제 종료

    internal void SetMark(string room) // 획득한 상점 표식 등록
    { // 상점 표식 등록 시작
        markRoom = room; // 원본 이벤트 방 기록
        markedShop = null; // 다음 실제 방문 대기
    } // 상점 표식 등록 종료

    internal void VisitShop(string shop) // 처음 방문한 상점에 할인 연결
    { // 상점 방문 기록 시작
        if (HasMark && string.IsNullOrEmpty(markedShop) && !string.IsNullOrWhiteSpace(shop)) // 아직 지정되지 않은 상점 확인
        { // 할인 상점 연결 시작
            markedShop = shop; // 다음 상점의 회차 방 식별자 보관
        } // 할인 상점 연결 종료
    } // 상점 방문 기록 종료

    public bool IsMarkedShop(string shop) // 미사용 표식이 묶인 상점 확인
    { // 표식 상점 확인 시작
        return HasMark && !string.IsNullOrEmpty(shop) && markedShop == shop; // 실제 처음 방문한 방 일치 확인
    } // 표식 상점 확인 종료

    public int OfferPrice(string shop, int price) // 표식이 적용된 상품 가격 계산
    { // 상품 가격 계산 시작
        return IsMarkedShop(shop) ? price - price / 10 : price; // 해당 상점의 표시가에서 할인액 내림
    } // 상품 가격 계산 종료

    internal void FinishPurchase(string shop) // 성공한 첫 상품 구매에만 할인 소모
    { // 할인 소모 시작
        if (IsMarkedShop(shop)) // 할인이 묶인 상점 확인
        { // 표식 사용 완료 시작
            markRoom = null; // 사용한 표식 제거
            markedShop = null; // 할인 상점 연결 해제
        } // 표식 사용 완료 종료
    } // 할인 소모 종료

    internal void LeaveFloor() // 이전 층 상점 연결만 해제
    { // 층 이동 처리 시작
        markedShop = null; // 다음 층 첫 상점까지 미구매 할인 보존
    } // 층 이동 처리 종료

    internal bool IsValid(string[] events, string[] cleared, int poolVersion) // 복원 전 저장 상태의 조합 검증
    { // 저장 상태 검증 시작
        bool emptyAmbush = string.IsNullOrEmpty(ambushRoom) && string.IsNullOrEmpty(normalEncounter); // 매복이 없는 기본 저장 확인
        bool validAmbush = !string.IsNullOrWhiteSpace(ambushRoom) && Array.IndexOf(events, ambushRoom) >= 0 && Array.IndexOf(cleared, AmbushBattleId) < 0 && BurntCartEvent.FindNormal(normalEncounter, poolVersion) != null; // 처리된 방과 유효한 미완료 전투 확인
        bool validMark = HasMark ? !string.IsNullOrWhiteSpace(markRoom) && Array.IndexOf(events, markRoom) >= 0 && (string.IsNullOrEmpty(markedShop) || !string.IsNullOrWhiteSpace(markedShop)) : string.IsNullOrEmpty(markedShop); // 표식 없는 할인이나 누락된 이벤트 방 차단
        return (emptyAmbush || validAmbush) && validMark; // 검증한 매복과 상점 표식 반환
    } // 저장 상태 검증 종료
} // 수레 상태 종료
