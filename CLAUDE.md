# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TFT(Teamfight Tactics) 방식의 오토배틀러 게임 프로토타입. Unity 6000.3.10f1, URP, C# 기반.

## Unity CLI 명령어
```
`unity-cli/README.md` 참조
```
## 아키텍처

### 핵심 데이터 흐름

```
HexGridLayout.LayoutGrid()
    → TileManager.RegisterTile()     ← 타일 딕셔너리 구축
    → TileManager.InitializeAllTiles()  ← TileScript.Initialize() 호출 (이웃 타일 계산)

TestScript.Start()
    → WaitForSeconds(0.2f)           ← 그리드 생성 대기 (타이머 기반, 개선 여지 있음)
    → UnitSpawner.SpawnUnit()        ← 유닛 생성 및 초기화
    → BattleManager.Instance.StartBattle()  ← OnBattleStart 이벤트 발동
        → UnitController.EnterIdleState()  ← 각 유닛 AI 시작
```

### 싱글톤 구조

| 클래스 | 타입 | 역할 |
|--------|------|------|
| `TileManager` | 순수 C# (`??=` 지연 생성) | 타일 딕셔너리 관리 |
| `UnitManager` | 순수 C# (`??=` 지연 생성) | 팀별 유닛 목록 관리 |
| `BattleManager` | MonoBehaviour (씬: Managers 하위) | 페이즈 관리, 전투 이벤트 발행 |

`TileManager`/`UnitManager`는 씬 오브젝트 없이 최초 접근 시 자동 생성된다.


## 주요 규칙
- 주석 상세히 작성
- 코드 변경 후 `unity-cli editor refresh --compile` 로 컴파일 확인
- 씬 조작(오브젝트 추가/삭제)은 Unity CLI 명령어로 수행
- `TileManager`/`UnitManager`는 씬에 오브젝트 배치 불필요 (순수 C# 싱글톤)
- 신규 MonoBehaviour 싱글톤 추가 시 `Managers` 하위에 배치
- 작업 계획은 `unity-cli/ToDoList.md` 참조 (`.gitignore` 제외 파일)
