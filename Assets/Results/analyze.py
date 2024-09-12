# libraries
import pandas as pd
import numpy as np
import networkx as nx
import matplotlib.pyplot as plt
import re
import os

class node_id_generator:
    def __init__(self):
        self.id = 0
    def next(self):
        self.id += 1
        return self.id

# node struct
class Node:
    def __init__(self, id, parent, volume, rotation, depth, visits, eval, bestRolloutDepth, bestRolloutExtractedPercentage):
        self.id = id
        self.parent = parent
        self.volume = volume
        self.rotation = rotation
        self.depth = depth
        self.visits = visits
        self.eval = eval
        self.bestRolloutDepth = bestRolloutDepth
        self.bestRolloutExtractedPercentage = bestRolloutExtractedPercentage

# Read the file
def analyze_hyperparameter_set(i):
    with open(f'{i}.txt') as f:
        lines = f.readlines()
        for i, line in enumerate(lines):
            if i == 0:
                print(line)
                continue

            if i in [25, 50, 100, 500, 1000]:
                def parseNode(nodeStr, parent, id_generator, nodes):
                    if not nodeStr:
                        return
                    # use regex to parse strings in the format: "volume=val,rotation=(valx,valy,valz),depth=val,visits=val,eval=val,bestRolloutDepth=val,bestRolloutExtractedPercentage=val,children=[child1,child2,...,childn,]" values are can be floats or integers
                    id = id_generator.next()
                    node_pattern = r'volume=([-?\d.]+),rotation=\(([-?\d.]+),([-?\d.]+),([-?\d.]+)\),depth=([-?\d.]+),visits=([-?\d.]+),eval=([-?\d.]+),bestRolloutDepth=([-?\d.]+),bestRolloutExtractedPercentage=([-?\d.]+),children=\[(.*)\]'
                    groups = re.compile(node_pattern).match(nodeStr).groups()
                    volume = float(groups[0])
                    rotation = groups[1:4]
                    depth = int(groups[4])
                    visits = int(groups[5])
                    eval = float(groups[6])
                    bestRolloutDepth = int(groups[7])
                    bestRolloutExtractedPercentage = float(groups[8])

                    node = Node(id, parent, volume, rotation, depth, visits, eval, bestRolloutDepth, bestRolloutExtractedPercentage)
                    nodes.append(node)

                    # use regex to parse the children of the following format: [node_parretn,node_parent,...,node_parent,]
                    childrenStr = groups[9]
                    children = []
                    bracket_level = 0
                    current_child = ''
                    prev_c = ''
                    for c in childrenStr:
                        if c == '[':
                            bracket_level += 1
                        elif c == ']':
                            bracket_level -= 1
                        if c == ',' and prev_c == ']' and bracket_level == 0:
                            children.append(current_child)
                            current_child = ''
                        else:
                            current_child += c
                        prev_c = c
                    for child in children:
                        parseNode(child, id, id_generator, nodes)

                id_generator = node_id_generator()
                nodes = []
                parseNode(line, None, id_generator, nodes)

                graph_nodes = {}
                connections_ids = [[], []]
                for node in nodes:
                    graph_nodes[node.id] = (f'volume={node.volume}\nvisits={node.visits}\neval={node.eval}\nrolloutDepth={node.bestRolloutDepth}\nrollout%={node.bestRolloutExtractedPercentage}\nid={node.id}\nrotation={node.rotation}', node.volume)
                    if node.parent is not None:
                        connections_ids[0].append(node.parent)
                        connections_ids[1].append(node.id)
                connections = [[], []]
                for i in range(len(connections_ids[0])):
                    connections[0].append(graph_nodes[connections_ids[0][i]][0])
                    connections[1].append(graph_nodes[connections_ids[1][i]][0])

                # Build a dataframe with your connections
                df = pd.DataFrame({ 'from':connections[0], 'to':connections[1]})

                # And a data frame with characteristics for your nodes
                nodes_tuples = list(graph_nodes.values())
                nodes_labels = [x[0] for x in nodes_tuples]
                nodes_values = [x[1] for x in nodes_tuples]
                carac = pd.DataFrame({ 'LABELS':nodes_labels, 'VALUES':nodes_values })

                # Build your graph
                G=nx.from_pandas_edgelist(df, 'from', 'to', create_using=nx.Graph() )
                carac= carac.set_index('LABELS')
                carac=carac.reindex(G.nodes())

                # Plot it, providing a continuous color scale with cmap:
                nx.draw(G, with_labels=True, node_color=carac['VALUES'].astype(float), cmap=plt.cm.cool, node_size=10000, font_size=8, font_color='black')
                plt.show()
                pass



def evaluate_hyperparameter_sets():
    best_after_1000 = []
    best_after_500 = []
    best_after_100 = []
    best_after_50 = []
    best_after_25 = []

    # open all files with the name format: i.txt
    files = [f'{i}.txt' for i in range(1, 335)]
    # remove missing files
    files = [f for f in files if os.path.exists(f)]
    for file in files:
        # Read the file
        with open(file) as f:
            lines = f.readlines()
            for i, line in enumerate(lines):
                if i == 0:
                    # print(line)
                    continue

                if i in [25, 50, 100, 500, 1000]:
                    node_pattern = r'volume=([-?\d.]+),rotation=\(([-?\d.]+),([-?\d.]+),([-?\d.]+)\),depth=([-?\d.]+),visits=([-?\d.]+),eval=([-?\d.]+),bestRolloutDepth=([-?\d.]+),bestRolloutExtractedPercentage=([-?\d.]+),children=\[(.*)\]'
                    groups = re.compile(node_pattern).match(line).groups()
                    volume = float(groups[0])
                    rotation = groups[1:4]
                    depth = int(groups[4])
                    visits = int(groups[5])
                    eval = float(groups[6])
                    bestRolloutDepth = int(groups[7])
                    bestRolloutExtractedPercentage = float(groups[8])

                    if i == 25:
                        best_after_25.append((file, bestRolloutDepth, bestRolloutExtractedPercentage))
                    elif i == 50:
                        best_after_50.append((file, bestRolloutDepth, bestRolloutExtractedPercentage))
                    elif i == 100:
                        best_after_100.append((file, bestRolloutDepth, bestRolloutExtractedPercentage))
                    elif i == 500:
                        best_after_500.append((file, bestRolloutDepth, bestRolloutExtractedPercentage))
                    elif i == 1000:
                        best_after_1000.append((file, bestRolloutDepth, bestRolloutExtractedPercentage))

    best_after_25.sort(key=lambda x: (-x[1], x[2]), reverse=True)
    best_after_50.sort(key=lambda x: (-x[1], x[2]), reverse=True)
    best_after_100.sort(key=lambda x: (-x[1], x[2]), reverse=True)
    best_after_500.sort(key=lambda x: (-x[1], x[2]), reverse=True)
    best_after_1000.sort(key=lambda x: (-x[1], x[2]), reverse=True)

    print('Best after 25')
    for i in range(10):
        print(best_after_25[i])
    print('Best after 50')
    for i in range(10):
        print(best_after_50[i])
    print('Best after 100')
    for i in range(10):
        print(best_after_100[i])
    print('Best after 500')
    for i in range(10):
        print(best_after_500[i])
    print('Best after 1000')
    for i in range(10):
        print(best_after_1000[i])



# evaluate_hyperparameter_sets()
analyze_hyperparameter_set(4)
