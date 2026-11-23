using System; // 기본 열거형 기능 사용
using System.Collections.Generic; // 목록 자료형 사용
using UnityEngine; // 유니티 로그 기능 사용
public sealed class BattleEventDebugLogger : IDisposable // 전투 공용 이벤트 순서 기록기
{ // 클래스 시작
    private readonly List<IDisposable> subscriptions = new List<IDisposable>(); // 로그 구독 해제 목록
    private bool disposed; // 기록기 종료 여부
    public BattleEventDebugLogger(BattleEventDispatcher dispatcher) // 이벤트 기록기 생성자
    { // 생성자 시작
        if (dispatcher == null) // 발행기 누락 확인
        { // 누락 처리 시작
            throw new ArgumentNullException(nameof(dispatcher)); // 발행기 누락 예외
        } // 누락 처리 종료
        foreach (BattleEventType eventType in Enum.GetValues(typeof(BattleEventType))) // 모든 이벤트 종류 순회
        { // 이벤트 로그 등록 시작
            subscriptions.Add(dispatcher.Subscribe(eventType, LogEvent, int.MinValue)); // 가장 낮은 우선순위 로그 구독
        } // 이벤트 로그 등록 종료
    } // 생성자 종료
    private static void LogEvent(BattleEventContext eventContext) // 공용 이벤트 내용 출력
    { // 이벤트 출력 시작
        string sourceName = eventContext.SourceUnit == null ? "없음" : eventContext.SourceUnit.DisplayName; // 발생 유닛 이름 계산
        string targetName = eventContext.TargetUnit == null ? "없음" : eventContext.TargetUnit.DisplayName; // 대상 유닛 이름 계산
        Debug.Log($"[BattleEvent] #{eventContext.Sequence} / {eventContext.EventType} / 라운드 {eventContext.Round} / 단계 {eventContext.Phase} / 발생 {sourceName} / 대상 {targetName} / 수치 {eventContext.AppliedAmount}"); // 공용 이벤트 순서 로그 출력
    } // 이벤트 출력 종료
    public void Dispose() // 이벤트 기록기 종료
    { // 기록기 종료 시작
        if (disposed) // 기존 종료 확인
        { // 중복 종료 처리 시작
            return; // 기록기 종료 중단
        } // 중복 종료 처리 종료
        disposed = true; // 기록기 종료 상태 저장
        foreach (IDisposable subscription in subscriptions) // 로그 구독 목록 순회
        { // 개별 구독 해제 시작
            subscription.Dispose(); // 로그 구독 해제
        } // 개별 구독 해제 종료
        subscriptions.Clear(); // 로그 구독 목록 초기화
    } // 기록기 종료 종료
} // 클래스 종료
