<h1 align="center">
    Gravity-Automata
    <img src="https://img.shields.io/badge/Unity-2022.3.52f1-blue?logo=unity&logoColor=white&style=for-the-badge">
</h1>

## Overview

Gravity-Automata simulates water flow and extraction in dynamic 2D and 3D environment grids. The project aims to accurately model water behavior under various gravity directions and visualize it using the marching cubes algorithm. It also features an AI agent optimized for the water extraction process, employing a modified Monte Carlo Tree Search (MCTS) algorithm. For an in-depth explanation of the project's development, refer to [the project report](https://github.com/SimBoi/Gravity-Automata/blob/main/project_report.pdf).

## Features

- **2D and 3D Water Simulation**: Simulate and visualize water flow and extraction in both 2D and 3D grids.
- **Dynamic Gravity Handling**: Model water behavior under different gravity directions.
- **Water Compression Physics**: Implements realistic water compression, where cells balance water volumes horizontally and excess water flows upwards to simulate natural water behavior.
- **Marching Cubes Visualization**: Render realistic 3D representations of the water simulation.
- **Custom Model Import**: Import and use your own 3D models (.obj format) to define simulation barriers.
- **Multithreading Support**: Optimize simulation performance with parallel processing.
- **AI Optimization**: Utilize a modified MCTS algorithm for optimizing water extraction processes.
- **User Interface**: Interactive UI for model import, environment configuration, and simulation control.

## Installation

1. **Download the Latest Executable**
   - Visit the [Releases page](https://github.com/SimBoi/Gravity-Automata/releases) to download the latest version of the executable.

2. **Run the Simulator**
   - Execute `GravityAutomata.exe` to start the simulation.

## Usage

1. **Import OBJ Model**: Upload your 3D model in .obj format.
2. **Configure Environment**: Select the imported model to configure its position, rotation, and scale. Set the grid size, simulation steps per second, and other parameters.
3. **Control Simulation**: Choose to manually rotate the grid or let the AI determine the optimal rotation. Press `<space>` to stop the AI at any time; the longer the AI runs, the better the solution is likely to be.

## 2D Demo

The 2D demo provides an easy way to explore the simulation and AI functionalities without needing custom models. It includes pre-configured grids for quick testing:

1. **Access the Demo**: Navigate to the [Releases page](https://github.com/SimBoi/Gravity-Automata/releases) to download the demo version.
2. **Try Pre-Configured Grids**: Experiment with different pre-configured grids to see how the simulation behaves and how the AI performs.

## Notes

- Ensure that the 3D model you import is a closed shape to ensure proper functionality of the scanline algorithm for barrier approximation.
- Ensure your 3D model is properly scaled to fit within the grid dimensions.
- The simulation performance may vary based on the grid size and other parameters.
- For detailed documentation and technical insights, refer to [the project report](https://github.com/SimBoi/Gravity-Automata/blob/main/project_report.pdf).
