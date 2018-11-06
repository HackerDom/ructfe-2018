#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include <string.h>

#define BLOCK_SIZE 16

const char LINEAR[BLOCK_SIZE * 2][8] = {
    {6, 15, 8, 19, 1, 19, 9, 14},
    {9, 19, 15, 11, 13, 18, 6, 12},
    {11, 8, 15, 4, 0, 20, 15, 4},
    {4, 3, 7, 16, 12, 14, 9, 4},
    {14, 1, 2, 2, 2, 9, 6, 20},
    {13, 11, 8, 7, 14, 8, 15, 5},
    {9, 16, 4, 10, 7, 1, 2, 1},
    {0, 7, 13, 2, 12, 12, 6, 11},
    {4, 6, 5, 18, 7, 3, 11, 20},
    {11, 3, 4, 17, 2, 18, 6, 3},
    {12, 13, 6, 19, 12, 18, 12, 17},
    {5, 18, 3, 3, 7, 19, 8, 14},
    {11, 18, 1, 9, 8, 10, 8, 11},
    {15, 5, 1, 10, 15, 17, 14, 17},
    {13, 7, 0, 7, 0, 12, 6, 14},
    {11, 18, 2, 14, 8, 19, 12, 11},
    {10, 8, 6, 20, 11, 12, 7, 14},
    {15, 14, 0, 15, 3, 20, 2, 9},
    {1, 9, 15, 6, 14, 18, 9, 3},
    {3, 20, 9, 10, 13, 8, 15, 7},
    {11, 10, 10, 1, 3, 3, 0, 10},
    {14, 12, 15, 5, 11, 9, 0, 9},
    {13, 18, 12, 7, 13, 19, 8, 14},
    {14, 5, 8, 18, 3, 9, 12, 11},
    {13, 6, 12, 17, 6, 14, 2, 12},
    {7, 4, 7, 8, 7, 20, 15, 4},
    {14, 7, 12, 7, 7, 7, 15, 5},
    {9, 6, 12, 9, 14, 5, 13, 16},
    {7, 8, 15, 14, 12, 13, 7, 9},
    {8, 10, 12, 19, 3, 6, 15, 1},
    {9, 20, 4, 9, 10, 8, 12, 3},
    {10, 1, 12, 15, 0, 20, 2, 17},
};

char buffer[BLOCK_SIZE * 2];

void compress(char* data) {
    for (int i = 0; i < BLOCK_SIZE; ++i) {
        buffer[i] = 0;
    }
    for (int i = 0; i < BLOCK_SIZE * 2; ++i) {
        for (int j = 0; j < 4; ++j) {
            buffer[LINEAR[i][j * 2]] += data[i] * LINEAR[i][j * 2 + 1];
        }
    }
    for (int i = 0; i < BLOCK_SIZE; ++i) {
        data[i + BLOCK_SIZE] = buffer[i];
    }
}

char* prepare_data(const char* data, size_t data_length) {
    size_t new_data_length = (size_t)(ceil(data_length / ((double)(BLOCK_SIZE))) * BLOCK_SIZE);
    char* new_data = malloc(new_data_length * sizeof(char*));
    for (size_t i = 0; i < data_length; ++i) {
        new_data[i] = data[i];
    }
    for (size_t i = data_length; i < new_data_length; ++i) {
        new_data[i] = 0;
    }
    return new_data;
}

void get_hash(const char* data, size_t data_length, char* out) {
    for (size_t i = 0; i < BLOCK_SIZE * 2; ++i) {
        buffer[i] = (char)i;
    }
    char* prepared_data = prepare_data(data, data_length);
    size_t new_data_length = (size_t)(ceil(data_length / ((double)(BLOCK_SIZE))) * BLOCK_SIZE);
    for (int j = 0; j < new_data_length / BLOCK_SIZE; ++j) {
        for (int i = 0; i < BLOCK_SIZE; ++i) {
            buffer[i] = prepared_data[i + j * BLOCK_SIZE];
        }
        compress(buffer);
    }
    for (int i = 0; i < BLOCK_SIZE; ++i) {
        out[i] = buffer[i + BLOCK_SIZE];
    }
    free(prepared_data);
}

