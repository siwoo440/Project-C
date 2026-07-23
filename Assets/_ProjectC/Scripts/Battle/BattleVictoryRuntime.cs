using System; // 이벤트 자료형 기능
using System.Collections.Generic; // List 자료형 기능
using UnityEngine; // Unity 기본 기능

public sealed class BattleVictoryRuntime : MonoBehaviour // 전투 승리 판정 관리자
{
    [Header("전투 시스템 연결")] // 전투 시스템 연결 구분
    [SerializeField] private BattleTargetSelectionRuntime targetSelectionRuntime; // 대상 선택 관리자

    [Header("승리 판정 대상")] // 승리 판정 대상 구분
    [SerializeField] private List<BattleUnit> enemyUnits = new List<BattleUnit>(); // 전투 적 목록

    public bool IsBattleWon { get; private set; } // 전투 승리 여부

    public event Action BattleWon; // 전투 승리 알림

    private void OnEnable() // 적 전투 불능 이벤트 연결
    {
        for (int index = 0; index < enemyUnits.Count; index++) // 적 목록 순회
        {
            BattleUnit enemyUnit = enemyUnits[index]; // 현재 적 가져오기

            if (enemyUnit == null) // 적 연결 여부 확인
            {
                continue; // 누락된 적 건너뛰기
            }

            enemyUnit.Defeated -= HandleEnemyDefeated; // 중복 이벤트 연결 방지
            enemyUnit.Defeated += HandleEnemyDefeated; // 전투 불능 이벤트 등록
        }
    }

    private void Start() // 승리 판정 초기화
    {
        IsBattleWon = false; // 시작 승리 상태 해제

        if (HasRegisteredEnemy() == false) // 등록된 적 존재 확인
        {
            Debug.LogError("No enemy unit is registered for victory checking.", this); // 적 목록 누락 오류
        }
    }

    private void OnDisable() // 적 전투 불능 이벤트 해제
    {
        for (int index = 0; index < enemyUnits.Count; index++) // 적 목록 순회
        {
            BattleUnit enemyUnit = enemyUnits[index]; // 현재 적 가져오기

            if (enemyUnit == null) // 적 연결 여부 확인
            {
                continue; // 누락된 적 건너뛰기
            }

            enemyUnit.Defeated -= HandleEnemyDefeated; // 전투 불능 이벤트 제거
        }
    }

    private void HandleEnemyDefeated(BattleUnit defeatedUnit) // 적 전투 불능 처리
    {
        if (defeatedUnit == null) // 전투 유닛 누락 확인
        {
            return; // 승리 검사 중단
        }

        if (defeatedUnit.UnitTeam != BattleUnitTeam.Enemy) // 적 진영 여부 확인
        {
            return; // 아군 전투 불능 처리 제외
        }

        CheckVictory(); // 전체 적 처치 여부 검사
    }

    private void CheckVictory() // 전투 승리 여부 검사
    {
        if (IsBattleWon) // 기존 승리 여부 확인
        {
            return; // 중복 승리 처리 중단
        }

        if (AreAllEnemiesDefeated() == false) // 전체 적 처치 여부 확인
        {
            return; // 남은 적 존재 시 중단
        }

        IsBattleWon = true; // 전투 승리 상태 설정

        if (targetSelectionRuntime != null) // 대상 선택 관리자 연결 확인
        {
            targetSelectionRuntime.CancelTargetSelection(); // 진행 중인 대상 선택 취소
        }

        Debug.Log("Battle victory reached.", this); // 전투 승리 결과 출력
        BattleWon?.Invoke(); // 전투 승리 이벤트 전달
    }

    private bool AreAllEnemiesDefeated() // 전체 적 전투 불능 여부 확인
    {
        bool validEnemyFound = false; // 유효한 적 발견 여부

        for (int index = 0; index < enemyUnits.Count; index++) // 적 목록 순회
        {
            BattleUnit enemyUnit = enemyUnits[index]; // 현재 적 가져오기

            if (enemyUnit == null) // 적 연결 여부 확인
            {
                continue; // 누락된 적 건너뛰기
            }

            if (enemyUnit.UnitTeam != BattleUnitTeam.Enemy) // 적 진영 여부 확인
            {
                continue; // 아군 유닛 건너뛰기
            }

            validEnemyFound = true; // 유효한 적 발견 설정

            if (enemyUnit.IsDefeated == false) // 현재 적 생존 여부 확인
            {
                return false; // 생존 적 존재 반환
            }
        }

        return validEnemyFound; // 유효한 적 존재와 전체 처치 결과 반환
    }

    private bool HasRegisteredEnemy() // 등록된 적 존재 여부 확인
    {
        for (int index = 0; index < enemyUnits.Count; index++) // 적 목록 순회
        {
            BattleUnit enemyUnit = enemyUnits[index]; // 현재 적 가져오기

            if (enemyUnit != null && enemyUnit.UnitTeam == BattleUnitTeam.Enemy) // 유효한 적 여부 확인
            {
                return true; // 등록된 적 존재 반환
            }
        }

        return false; // 등록된 적 없음 반환
    }
}